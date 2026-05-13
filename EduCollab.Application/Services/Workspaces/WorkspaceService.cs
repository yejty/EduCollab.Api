using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Repositories.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUserRepository _userRepository;

        public WorkspaceService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<WorkspaceMember>> GetWorkspaceUsersAsync(int id, CancellationToken cancellationToken)
        {
            // TODO: Load workspace users with role/groups/join metadata; return 404 from controller if workspace is missing.
            var response = new List<WorkspaceMember>();
            return Task.FromResult(response);
        }

        public Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            // TODO: Resolve membership by workspace + user; map role, groups, JoinedAt from persistence.
            return Task.FromResult<WorkspaceMember?>(null);
        }

        public Task<bool> CreateUserInWorkspaceAsync(User user, string password, string invitationToken, CancellationToken cancellationToken)
        {
            // TODO : workspace
            return Task.FromResult(true);
        }

        public Task InviteUserToWorkspaceAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));
            // TODO : workspace
            return Task.CompletedTask;
        }

        public Task<bool> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
