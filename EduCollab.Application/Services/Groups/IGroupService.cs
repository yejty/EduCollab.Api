using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Groups
{
    public interface IGroupService
    {
        Task<bool> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken);
        Task<GroupMember?> GetCurrentUserGroupMemberAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<List<GroupMember>> GetAllGroupMembersAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<GroupMember?> GetGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken);
        Task<GroupMember?> CreateGroupMemberAsync(int workspaceId, int groupId, GroupMember member, CancellationToken cancellationToken);
        Task<GroupMember?> UpdateGroupMemberAsync(int workspaceId, int groupId, int userId, GroupRole role, CancellationToken cancellationToken);
        Task<bool> DeleteGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken);
    }
}
