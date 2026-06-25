using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IGroupRepository
    {
        Task<int> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Group>> GetGroupsForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
        Task<List<Group>> GetChildGroupsAsync(int workspaceId, int? parentGroupId, CancellationToken cancellationToken);
        Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken);
        Task<List<GroupMember>> GetAllGroupMembersAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<GroupMember?> GetGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken);
        Task<GroupMember?> CreateGroupMemberAsync(int workspaceId, GroupMember member, CancellationToken cancellationToken);
        Task<bool> DeleteGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken);
        Task<List<int>> GetUserGroupIdsAsync(int workspaceId, int userId, CancellationToken cancellationToken);
    }
}
