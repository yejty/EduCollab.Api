using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public class WorkspaceService : IWorkspaceService
    {
        public Task<Workspace> GetWorkspaceAsync(int id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<WorkspaceUsersResponse> GetWorkspaceUsersAsync(long workspaceId, CancellationToken cancellationToken)
        {
            // TODO: Load workspace_members joined with Users when workspaces schema exists; return 404 from controller if workspace missing.
            var response = new WorkspaceUsersResponse { Users = new List<WorkspaceUserResponse>() };
            return Task.FromResult(response);
        }

        public Task<WorkspaceUsersResponse> GetWorkspaceUsersAsync(int id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateUserAsync(User user, string password, string invitationToken, CancellationToken cancellationToken)
        {
            // TODO : workspace
          //return _userRepository.CreateAsync(user, password, invitationToken, cancellationToken);
        }

        public Task InviteAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));
            // TODO : workspace
          //return _userRepository.InviteAsync(email, cancellationToken);
        }
    }
}
