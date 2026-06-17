using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IAssetFolderRepository _assetFolderRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public GroupService(
            IGroupRepository groupRepository,
            IAssetFolderRepository assetFolderRepository,
            IAssetRepository assetRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _groupRepository = groupRepository;
            _assetFolderRepository = assetFolderRepository;
            _assetRepository = assetRepository;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private async Task<(int WorkspaceId, WorkspaceMember Membership)> ResolveCurrentWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var currentUserId = RequireCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(currentUserId, cancellationToken);
            if (user?.WorkspaceId is not int workspaceId || workspaceId <= 0)
                throw new AccessDeniedException("You must belong to a workspace to access groups.");

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, currentUserId, cancellationToken);
            if (membership is null)
                throw new AccessDeniedException("You must be a member of this workspace to access its groups.");

            return (workspaceId, membership);
        }

        public async Task<WorkspaceRole> GetCurrentUserWorkspaceRoleAsync(CancellationToken cancellationToken)
        {
            var (_, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            return membership.Role;
        }

        private async Task EnsureCurrentUserCanAccessGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var (_, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (workspaceId != workspaceMember.WorkspaceId)
                throw new AccessDeniedException("You cannot access groups outside your workspace.");

            if (WorkspaceRolePermissions.CanSeeAllContent(workspaceMember.Role))
                return;

            var currentUserId = RequireCurrentUserId();
            var groupMembership = await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, currentUserId, cancellationToken);
            if (groupMembership is null)
                throw new AccessDeniedException("You must be a member of this group to access it.");
        }

        private async Task EnsureCurrentUserCanManageGroupsAsync(CancellationToken cancellationToken)
        {
            var (_, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanManageGroups(workspaceMember.Role))
                throw new AccessDeniedException("Only workspace owners and managers can manage groups.");
        }

        private async Task EnsureCurrentUserCanManageGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            await EnsureCurrentUserCanManageGroupsAsync(cancellationToken);

            if (workspaceId != (await ResolveCurrentWorkspaceMembershipAsync(cancellationToken)).WorkspaceId)
                throw new AccessDeniedException("You cannot manage groups outside your workspace.");

            await RequireGroupAsync(workspaceId, groupId, cancellationToken);
        }

        private async Task<Group> RequireGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");

            return group;
        }

        private async Task<List<AssetFolder>> GetAllFoldersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            var rootFolders = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, null, cancellationToken);
            var result = new List<AssetFolder>(rootFolders);
            var queue = new Queue<AssetFolder>(rootFolders);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, current.Id, cancellationToken);
                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child);
                }
            }

            return result;
        }

        private static HashSet<int> BuildVisibleFolderIds(IEnumerable<AssetFolder> folders, IEnumerable<AssetFolderGroupShare> shares)
        {
            var sharedFolderIds = shares.Select(s => s.FolderId).ToHashSet();
            if (sharedFolderIds.Count == 0)
                return new HashSet<int>();

            var foldersById = folders.ToDictionary(folder => folder.Id);
            var visibleFolderIds = new HashSet<int>();

            foreach (var folder in foldersById.Values)
            {
                var current = folder;
                while (true)
                {
                    if (sharedFolderIds.Contains(current.Id))
                    {
                        visibleFolderIds.Add(folder.Id);
                        break;
                    }

                    if (current.ParentFolderId is not int parentId || !foldersById.TryGetValue(parentId, out var parent))
                        break;

                    current = parent;
                }
            }

            return visibleFolderIds;
        }

        private async Task<(int WorkspaceId, HashSet<int> VisibleFolderIds, List<AssetFolder> Folders)> ResolveVisibleFoldersAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await RequireGroupAsync(workspaceId, groupId, cancellationToken);

            if (!WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);

            var folders = await GetAllFoldersAsync(workspaceId, cancellationToken);

            var shares = await _assetFolderRepository.GetAssetFolderSharesByGroupAsync(workspaceId, groupId, cancellationToken);
            var visibleFolderIds = WorkspaceRolePermissions.CanSeeAllContent(membership.Role)
                ? folders.Select(f => f.Id).ToHashSet()
                : BuildVisibleFolderIds(folders, shares);

            return (workspaceId, visibleFolderIds, folders);
        }

        public async Task<bool> CreateGroupAsync(Group group, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            var (workspaceId, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanManageGroups(membership.Role))
                throw new AccessDeniedException("Only workspace owners and managers can create groups.");

            var now = DateTimeOffset.UtcNow;
            group.CreatedAtUtc = now.UtcDateTime;
            group.UpdatedAtUtc = now.UtcDateTime;
            group.CreatedByUserId = RequireCurrentUserId();
            group.UserCount = 1;
            
            var groupId = await _groupRepository.CreateGroupAsync(workspaceId, group, cancellationToken);
            if (groupId <= 0)
                return false;
            group.Id = groupId;
            return true;
        }

        public async Task<bool> DeleteGroupAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanManageGroupAsync(workspaceId, groupId, cancellationToken);
            return await _groupRepository.DeleteGroupAsync(workspaceId, groupId, cancellationToken); 
        }

        public async Task<List<Group>> GetAllGroupsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);

            return await _groupRepository.GetGroupsForMemberAsync(workspaceId, membership.UserId, cancellationToken);
        }

        public async Task<Group?> GetGroupByIdAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            return await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
        }

        public async Task<Group?> UpdateGroupAsync(Group group, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);

            if (group.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(group.Id));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanManageGroupAsync(workspaceId, group.Id, cancellationToken);

            var existing = await _groupRepository.GetGroupByIdAsync(workspaceId, group.Id, cancellationToken);
            if (existing is null)
                return null;

            group.CreatedAtUtc = existing.CreatedAtUtc;
            group.CreatedByUserId = existing.CreatedByUserId;
            group.UserCount = existing.UserCount;
            return await _groupRepository.UpdateGroupAsync(workspaceId, group, cancellationToken);
        }

        public async Task<GroupMember?> GetCurrentUserGroupMemberAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            return await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, workspaceMember.UserId, cancellationToken);
        }

        public async Task<List<GroupMember>> GetAllGroupMembersAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            return await _groupRepository.GetAllGroupMembersAsync(workspaceId, groupId, cancellationToken);
        }

        public async Task<GroupMember?> GetGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            return await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
        }

        public async Task<GroupMember?> CreateGroupMemberAsync(int groupId, GroupMember member, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            ArgumentNullException.ThrowIfNull(member);
            if (member.UserId <= 0)
                throw new ArgumentOutOfRangeException(nameof(member.UserId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanManageGroupAsync(workspaceId, groupId, cancellationToken);

            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");

            var isWorkspaceMember = await _workspaceRepository.IsUserWorkspaceMemberAsync(workspaceId, member.UserId, cancellationToken);
            if (!isWorkspaceMember)
                throw new ArgumentException("User must be a member of the workspace before joining the group.");

            member.GroupId = groupId;
            member.JoinedAtUtc = DateTime.UtcNow;
            return await _groupRepository.CreateGroupMemberAsync(workspaceId, member, cancellationToken);
        }

        public async Task<bool> DeleteGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            var (workspaceId, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            var isSelf = workspaceMember.UserId == userId;

            if (!WorkspaceRolePermissions.CanManageGroups(workspaceMember.Role))
            {
                if (!isSelf)
                    throw new AccessDeniedException("Only workspace owners and managers can remove other group members.");

                await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            }
            else if (!isSelf)
            {
                await RequireGroupAsync(workspaceId, groupId, cancellationToken);
            }
            else
            {
                await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            }

            return await _groupRepository.DeleteGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
        }

        public async Task<List<AssetFolder>> GetVisibleRootAssetFoldersAsync(int groupId, CancellationToken cancellationToken)
        {
            var (_, visibleFolderIds, folders) = await ResolveVisibleFoldersAsync(groupId, cancellationToken);
            return folders
                .Where(folder => visibleFolderIds.Contains(folder.Id)
                    && (folder.ParentFolderId is null || !visibleFolderIds.Contains(folder.ParentFolderId.Value)))
                .OrderBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(folder => folder.Id)
                .ToList();
        }

        public async Task<List<AssetFolder>> GetVisibleSubFoldersAsync(int groupId, int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (_, visibleFolderIds, folders) = await ResolveVisibleFoldersAsync(groupId, cancellationToken);
            if (!visibleFolderIds.Contains(folderId))
                throw new KeyNotFoundException("Asset folder not found.");

            return folders
                .Where(folder => folder.ParentFolderId == folderId && visibleFolderIds.Contains(folder.Id))
                .OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(folder => folder.Id)
                .ToList();
        }

        public async Task<List<Asset>> GetVisibleAssetsInFolderAsync(int groupId, int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, visibleFolderIds, _) = await ResolveVisibleFoldersAsync(groupId, cancellationToken);
            if (!visibleFolderIds.Contains(folderId))
                throw new KeyNotFoundException("Asset folder not found.");

            var assets = await _assetRepository.GetAssetsByFolderAsync(workspaceId, folderId, cancellationToken);
            return assets
                .OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.Id)
                .ToList();
        }

        public async Task<List<Asset>> GetVisibleRootAssetsAsync(int groupId, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await RequireGroupAsync(workspaceId, groupId, cancellationToken);

            if (!WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);

            var (_, visibleFolderIds, _) = await ResolveVisibleFoldersAsync(groupId, cancellationToken);
            var sharedAssetIds = (await _assetRepository.GetAssetSharesByGroupAsync(workspaceId, groupId, cancellationToken))
                .Select(share => share.AssetId)
                .ToHashSet();

            var assets = await _assetRepository.GetAllAssetsAsync(workspaceId, cancellationToken);

            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
            {
                return assets
                    .OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(asset => asset.Id)
                    .ToList();
            }

            return assets
                .Where(asset =>
                    sharedAssetIds.Contains(asset.Id)
                    || (asset.FolderId is int folderId && visibleFolderIds.Contains(folderId)))
                .Where(asset => asset.FolderId is null || !visibleFolderIds.Contains(asset.FolderId.Value))
                .OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.Id)
                .ToList();
        }
    }
}
