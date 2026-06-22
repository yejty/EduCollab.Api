using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Application.Services.Assets
{
    public class AssetFolderService : IAssetFolderService
    {
        private readonly IAssetFolderRepository _assetFolderRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public AssetFolderService(
            IAssetFolderRepository assetFolderRepository,
            IGroupRepository groupRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _assetFolderRepository = assetFolderRepository;
            _groupRepository = groupRepository;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private static string ValidateAndNormalizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name is required.", nameof(name));

            var normalized = name.Trim();
            if (normalized.Contains('/'))
                throw new ArgumentException("Folder name cannot contain '/'.", nameof(name));

            return normalized;
        }

        private Task<(int WorkspaceId, WorkspaceMember Membership)> RequireWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            return CurrentWorkspaceAccess.RequireMembershipAsync(
                _userRepository,
                _workspaceRepository,
                userId,
                cancellationToken);
        }

        private static void EnsureCanManageAssetFolders(WorkspaceMember membership)
        {
            if (!WorkspaceRolePermissions.CanManageAssetFolders(membership.Role))
                throw new AccessDeniedException("Only the workspace owner can manage asset folders.");
        }

        private async Task<int> EnsureCanManageAssetFoldersAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanManageAssetFolders(membership);
            return workspaceId;
        }

        private async Task<(int WorkspaceId, WorkspaceMember Membership)> EnsureCanShareContentAsync(CancellationToken cancellationToken)
        {
            var result = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanShareContent(result.Membership.Role))
                throw new AccessDeniedException("Only workspace owners and managers can share content with groups.");

            return result;
        }

        private async Task<(AssetFolder? Parent, string Path)> ResolvePathAsync(int workspaceId, int? parentFolderId, string name, CancellationToken cancellationToken)
        {
            if (parentFolderId is null)
            {
                return (null, "/" + name);
            }

            var parent = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, parentFolderId.Value, cancellationToken);
            if (parent is null)
                throw new KeyNotFoundException("Parent folder was not found.");

            return (parent, parent.Path.TrimEnd('/') + "/" + name);
        }

        private async Task EnsureGroupBelongsToWorkspaceAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");
        }

        private async Task EnsureCanShareWithGroupOnCreateAsync(int workspaceId, int groupId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanShareContent(membership.Role))
                return;

            var groupMember = await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
            if (groupMember is null)
                throw new AccessDeniedException("You must belong to the selected group to share with it.");
        }

        private sealed record FolderVisibilityContext(
            bool CanSeeAllContent,
            int UserId,
            HashSet<int> VisibleFolderIds);

        private async Task<List<AssetFolder>> GetAllFoldersUnfilteredAsync(int workspaceId, CancellationToken cancellationToken)
        {
            var folders = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, null, cancellationToken);
            var result = new List<AssetFolder>(folders);
            var queue = new Queue<AssetFolder>(folders);

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

        private async Task<FolderVisibilityContext> BuildFolderVisibilityContextAsync(
            int workspaceId,
            WorkspaceMember membership,
            int userId,
            CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return new FolderVisibilityContext(true, userId, []);

            var userGroupIds = (await _groupRepository.GetUserGroupIdsAsync(workspaceId, userId, cancellationToken)).ToHashSet();
            var folders = await GetAllFoldersUnfilteredAsync(workspaceId, cancellationToken);
            var folderShares = await _assetFolderRepository.GetWorkspaceAssetFolderSharesAsync(workspaceId, cancellationToken);
            var visibleFolderIds = WorkspaceContentVisibility.BuildVisibleFolderIds(folders, folderShares, userGroupIds);
            return new FolderVisibilityContext(false, userId, visibleFolderIds);
        }

        private static bool IsFolderVisible(AssetFolder folder, FolderVisibilityContext context) =>
            WorkspaceContentVisibility.IsFolderVisibleToUser(
                folder,
                context.UserId,
                context.CanSeeAllContent,
                context.VisibleFolderIds);

        public async Task<bool> CreateAssetFolderAsync(AssetFolder folder, int groupId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(folder);

            if (groupId <= 0)
                throw new ArgumentException("GroupId is required.", nameof(groupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanManageAssetFolders(membership);

            var normalizedName = ValidateAndNormalizeFolderName(folder.Name);
            var (_, path) = await ResolvePathAsync(workspaceId, folder.ParentFolderId, normalizedName, cancellationToken);

            folder.WorkspaceId = workspaceId;
            folder.Name = normalizedName;
            folder.Path = path;
            var userId = RequireCurrentUserId();
            folder.CreatedByUserId = userId;
            folder.CreatedAtUtc = DateTime.UtcNow;
            folder.UpdatedAtUtc = folder.CreatedAtUtc;

            var id = await _assetFolderRepository.CreateAssetFolderAsync(workspaceId, folder, cancellationToken);
            if (id <= 0)
                return false;

            folder.Id = id;

            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            await EnsureCanShareWithGroupOnCreateAsync(workspaceId, groupId, membership, userId, cancellationToken);

            var share = new AssetFolderGroupShare
            {
                FolderId = id,
                GroupId = groupId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _assetFolderRepository.CreateAssetFolderShareAsync(workspaceId, share, cancellationToken);

            return true;
        }

        public async Task<List<AssetFolder>> GetRootAssetFoldersAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var folders = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, null, cancellationToken);
            var context = await BuildFolderVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return folders.Where(folder => IsFolderVisible(folder, context)).ToList();
        }

        public async Task<List<AssetFolder>> GetAllAssetFoldersAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var result = await GetAllFoldersUnfilteredAsync(workspaceId, cancellationToken);
            var context = await BuildFolderVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return result
                .Where(folder => IsFolderVisible(folder, context))
                .OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Id)
                .ToList();
        }

        public async Task<List<AssetFolder>> GetSubFoldersAsync(int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            var folder = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (folder is null)
                throw new KeyNotFoundException("Asset folder not found.");

            var context = await BuildFolderVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            if (!IsFolderVisible(folder, context))
                throw new KeyNotFoundException("Asset folder not found.");

            var children = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, folderId, cancellationToken);
            return children.Where(child => IsFolderVisible(child, context)).ToList();
        }

        public async Task<AssetFolder?> GetAssetFolderByIdAsync(int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            var folder = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (folder is null)
                return null;

            var context = await BuildFolderVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return IsFolderVisible(folder, context) ? folder : null;
        }

        public async Task<AssetFolder?> UpdateAssetFolderAsync(AssetFolder folder, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(folder);
            if (folder.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(folder.Id));

            var workspaceId = await EnsureCanManageAssetFoldersAsync(cancellationToken);

            var existing = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folder.Id, cancellationToken);
            if (existing is null)
                return null;

            var normalizedName = ValidateAndNormalizeFolderName(folder.Name);
            var oldPath = existing.Path;

            if (folder.ParentFolderId == folder.Id)
                throw new ArgumentException("A folder cannot be its own parent.");

            var (parent, newPath) = await ResolvePathAsync(workspaceId, folder.ParentFolderId, normalizedName, cancellationToken);
            if (parent is not null && parent.Path.StartsWith(oldPath + "/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A folder cannot be moved under one of its descendants.");

            existing.ParentFolderId = folder.ParentFolderId;
            existing.Name = normalizedName;
            existing.Path = newPath;

            var updated = await _assetFolderRepository.UpdateAssetFolderAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            if (!string.Equals(oldPath, updated.Path, StringComparison.Ordinal))
            {
                await _assetFolderRepository.UpdateDescendantPathsAsync(workspaceId, oldPath, updated.Path, cancellationToken);
            }

            return updated;
        }

        public async Task<bool> DeleteAssetFolderAsync(int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var workspaceId = await EnsureCanManageAssetFoldersAsync(cancellationToken);

            var existing = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (existing is null)
                return false;

            return await _assetFolderRepository.DeleteAssetFolderAsync(workspaceId, folderId, cancellationToken);
        }

        public async Task<bool> ShareAssetFolderAsync(int folderId, int groupId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, membership) = await EnsureCanShareContentAsync(cancellationToken);
            var existing = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (existing is null)
                return false;

            var userId = RequireCurrentUserId();
            if (!WorkspaceRolePermissions.CanShareContent(membership.Role) && existing.CreatedByUserId != userId)
                throw new AccessDeniedException("Only workspace owners and managers can share content with groups.");

            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);

            var share = new AssetFolderGroupShare
            {
                FolderId = folderId,
                GroupId = groupId,
                CreatedByUserId = RequireCurrentUserId(),
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _assetFolderRepository.CreateAssetFolderShareAsync(workspaceId, share, cancellationToken);
            return created is not null;
        }

        public async Task<bool> RemoveAssetFolderShareAsync(int folderId, int groupId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, _) = await EnsureCanShareContentAsync(cancellationToken);
            var existing = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            return await _assetFolderRepository.DeleteAssetFolderShareAsync(workspaceId, folderId, groupId, cancellationToken);
        }

        public async Task<List<int>> GetAssetFolderGroupIdsAsync(int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var shares = await _assetFolderRepository.GetAssetFolderSharesAsync(workspaceId, folderId, cancellationToken);
            return shares.Select(s => s.GroupId).ToList();
        }

        public async Task<bool> CanCurrentUserManageWorkspaceAssetsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
                return WorkspaceRolePermissions.CanManageAssetFolders(membership.Role);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (AccessDeniedException)
            {
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }
    }
}
