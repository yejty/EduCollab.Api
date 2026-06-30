using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Groups
{
    public interface IGroupService
    {
        Task<bool> CreateGroupAsync(Group group, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<List<Group>> GetAllGroupsAsync(CancellationToken cancellationToken);
        Task<List<Group>> GetAccessibleGroupsAsync(int? parentGroupId, CancellationToken cancellationToken);
        Task<List<Group>> GetAccessibleGroupsFlatAsync(CancellationToken cancellationToken);
        Task<Group?> GetGroupByIdAsync(int groupId, CancellationToken cancellationToken);
        Task<Group?> UpdateGroupAsync(Group group, CancellationToken cancellationToken);
        Task<WorkspaceRole> GetCurrentUserWorkspaceRoleAsync(CancellationToken cancellationToken);
        Task<GroupMember?> GetCurrentUserGroupMemberAsync(int groupId, CancellationToken cancellationToken);
        Task<List<GroupMember>> GetAllGroupMembersAsync(int groupId, CancellationToken cancellationToken);
        Task<GroupMember?> GetGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken);
        Task<GroupMember?> CreateGroupMemberAsync(int groupId, GroupMember member, CancellationToken cancellationToken);
        Task<bool> DeleteGroupMemberAsync(int groupId, int userId, CancellationToken cancellationToken);
    }
}
