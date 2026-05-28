using EduCollab.Application.Identity;
using EduCollab.Application.Repositories;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public GroupService(IGroupRepository groupRepository, IWorkspaceRepository workspaceRepository, IUserRepository userRepository, ICurrentUser currentUser)
        {
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

        private static bool CanManageWorkspace(WorkspaceRole role) =>
            role is WorkspaceRole.Owner or WorkspaceRole.Admin;

        private async Task EnsureCurrentUserCanAccessGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var (_, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (workspaceId != workspaceMember.WorkspaceId)
                throw new AccessDeniedException("You cannot access groups outside your workspace.");
            if (CanManageWorkspace(workspaceMember.Role))
                return;

            var currentUserId = RequireCurrentUserId();
            var groupMembership = await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, currentUserId, cancellationToken);
            if (groupMembership is null)
                throw new AccessDeniedException("You must be a member of this group to access it.");
        }

        private async Task EnsureCurrentUserCanManageGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var (_, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (workspaceId != workspaceMember.WorkspaceId)
                throw new AccessDeniedException("You cannot manage groups outside your workspace.");
            if (CanManageWorkspace(workspaceMember.Role))
                return;

            var currentUserId = RequireCurrentUserId();
            var groupMembership = await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, currentUserId, cancellationToken);
            if (groupMembership is null)
                throw new AccessDeniedException("You must be a member of this group to manage it.");

            if (groupMembership.Role != GroupRole.Admin)
                throw new AccessDeniedException("Only group admins can manage this group.");
        }

        private async Task EnsureCurrentUserIsGroupMemberAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
        }

        private async Task EnsureCurrentUserIsGroupAdminAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            await EnsureCurrentUserCanManageGroupAsync(workspaceId, groupId, cancellationToken);
        }

        public async Task<bool> CreateGroupAsync(Group group, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);

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
            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            return await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);
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
            {
                return null;
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
            await EnsureCurrentUserIsGroupMemberAsync(workspaceId, groupId, cancellationToken);
            return await _groupRepository.GetAllGroupMembersAsync(workspaceId, groupId, cancellationToken);
        }

        public async Task<GroupMember?> GetGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserIsGroupMemberAsync(workspaceId, groupId, cancellationToken);
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
            return await _groupRepository.CreateGroupMemberAsync(workspaceId, member, cancellationToken);
        }

        public async Task<GroupMember?> UpdateGroupMemberAsync(int groupId, int userId, GroupRole role, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            var (workspaceId, _) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            await EnsureCurrentUserCanManageGroupAsync(workspaceId, groupId, cancellationToken);
            return await _groupRepository.UpdateGroupMemberAsync(workspaceId, groupId, userId, role, cancellationToken);
        }

        public async Task<bool> DeleteGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            var (workspaceId, workspaceMember) = await ResolveCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!CanManageWorkspace(workspaceMember.Role))
            {
                if (workspaceMember.UserId != userId)
                    await EnsureCurrentUserCanManageGroupAsync(workspaceId, groupId, cancellationToken);
                else
                    await EnsureCurrentUserCanAccessGroupAsync(workspaceId, groupId, cancellationToken);
            }

            return await _groupRepository.DeleteGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
        }
    }
}
