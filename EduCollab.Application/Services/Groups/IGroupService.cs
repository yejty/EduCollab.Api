using EduCollab.Application.Models.Groups;

namespace EduCollab.Application.Services.Groups
{
    public interface IGroupService
    {
        Task<bool> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken);
    }
}
