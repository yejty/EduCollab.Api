using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Application.Services.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupAccessResolver _groupAccessResolver;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public GroupService(
            IGroupRepository groupRepository,
            IGroupAccessResolver groupAccessResolver,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _groupRepository = groupRepository;
            _groupAccessResolver = groupAccessResolver;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private Task<(int WorkspaceId, WorkspaceMember Membership)> ResolveCurrentWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var currentUserId = RequireCurrentUserId();
            return CurrentWorkspaceAccess.RequireMembershipAsync(
                _userRepository,
                _workspaceRepository,
                currentUserId,
                cancellationToken);
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
            if (await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, currentUserId, groupId, cancellationToken))
                return;

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

        private async Task EnsureCurrentUserCanManageGroupMembersAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var (_, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);

            if (workspaceId != workspaceMember.WorkspaceId)
                throw new AccessDeniedException("You cannot manage groups outside your workspace.");

            await RequireGroupAsync(workspaceId, groupId, cancellationToken);

            if (workspaceMember.Role == WorkspaceRole.Owner)
                return;

            if (workspaceMember.Role == WorkspaceRole.Manager)
            {
                var currentUserGroupMember = await _groupRepository.GetGroupMemberAsync(
                    workspaceId,
                    groupId,
                    workspaceMember.UserId,
                    cancellationToken);
                if (currentUserGroupMember is not null)
                    return;
            }

            throw new AccessDeniedException("Only workspace owners or managers who are members of this group can manage group members.");
        }

        private async Task<Group> RequireGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");

            return group;
        }

        private static void AssignGroupPaths(IReadOnlyList<Group> groups)
        {
            var groupsById = groups.ToDictionary(g => g.Id);
            foreach (var group in groups)
            {
                var segments = new List<string>();
                var current = group;
                var visited = new HashSet<int>();

                while (true)
                {
                    if (!visited.Add(current.Id))
                        break;

                    segments.Add(current.Name);
                    if (current.ParentGroupId is not int parentId || !groupsById.TryGetValue(parentId, out var parent))
                        break;

                    current = parent;
                }

                segments.Reverse();
                group.Path = "/" + string.Join("/", segments);
            }
        }

        public async Task<bool> CreateGroupAsync(Group group, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            var (workspaceId, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanManageGroups(membership.Role))
                throw new AccessDeniedException("Only workspace owners and managers can create groups.");

            if (group.ParentGroupId is int parentGroupId)
                await RequireGroupAsync(workspaceId, parentGroupId, cancellationToken);

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
            var groups = WorkspaceRolePermissions.CanSeeAllContent(membership.Role)
                ? await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken)
                : await _groupRepository.GetGroupsForMemberAsync(workspaceId, membership.UserId, cancellationToken);

            AssignGroupPaths(groups);
            return groups;
        }

        public async Task<List<Group>> GetAccessibleGroupsAsync(int? parentGroupId, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (parentGroupId is int parentId)
            {
                await EnsureCurrentUserCanAccessGroupAsync(workspaceId, parentId, cancellationToken);
                var children = await _groupRepository.GetChildGroupsAsync(workspaceId, parentId, cancellationToken);
                if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                {
                    AssignGroupPaths(children);
                    return children;
                }

                var accessibleGroupIds = await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(workspaceId, userId, cancellationToken);
                var filteredChildren = children.Where(g => accessibleGroupIds.Contains(g.Id)).ToList();
                AssignGroupPaths(filteredChildren);
                return filteredChildren;
            }

            var allGroups = await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
            {
                var roots = allGroups.Where(g => g.ParentGroupId is null).ToList();
                AssignGroupPaths(roots);
                return roots;
            }

            var accessibleIds = await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(workspaceId, userId, cancellationToken);
            var browseRoots = allGroups
                .Where(g => accessibleIds.Contains(g.Id))
                .Where(g => g.ParentGroupId is not int parentGroupIdValue
                    || !accessibleIds.Contains(parentGroupIdValue))
                .ToList();
            AssignGroupPaths(browseRoots);
            return browseRoots;
        }

        public async Task<Group?> GetGroupByIdAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                return null;

            var allGroups = await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);
            AssignGroupPaths(allGroups);
            return group;
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

            if (group.ParentGroupId is int parentGroupId)
            {
                if (parentGroupId == group.Id)
                    throw new ArgumentException("A group cannot be its own parent.");

                await RequireGroupAsync(workspaceId, parentGroupId, cancellationToken);
            }

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
            await EnsureCurrentUserCanManageGroupMembersAsync(workspaceId, groupId, cancellationToken);

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

            if (isSelf)
            {
                await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            }
            else
            {
                await EnsureCurrentUserCanManageGroupMembersAsync(workspaceId, groupId, cancellationToken);
            }

            return await _groupRepository.DeleteGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
        }
    }
}
