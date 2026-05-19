using EduCollab.Application.Models.Groups;

namespace EduCollab.Application.Services.Groups
{
    public class GroupService : IGroupService
    {
        public Task<bool> CreateGroupAsync(Group group, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
