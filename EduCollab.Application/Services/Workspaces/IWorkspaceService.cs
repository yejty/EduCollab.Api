using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<bool> CreateUserInWorkspaceAsync(User user, string password, string invitationToken, CancellationToken cancellationToken);
        Task InviteUserToWorkspaceAsync(string email, CancellationToken cancellationToken);
        Task<Workspace> GetWorkspaceAsync(int id, CancellationToken cancellationToken);
        Task<WorkspaceMembersResponse> GetWorkspaceUsersAsync(int id, CancellationToken cancellationToken);
        Task<WorkspaceMemberResponse?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
        Task<bool> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken);
    }
}
