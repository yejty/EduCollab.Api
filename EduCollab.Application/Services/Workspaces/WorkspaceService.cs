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
        public async Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken)
        {
            return await _userRepository.GetWorkspaceByIdAsync(id, cancellationToken);
        }

        public async Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int id, CancellationToken cancellationToken)
        {
            return await _userRepository.GetWorkspaceMembersAsync(id, cancellationToken);
        }

        public async Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return await _userRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
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
