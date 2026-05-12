using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task CreateUserAsync(User user, string password, string invitationToken, CancellationToken cancellationToken);
        Task InviteAsync(string email, CancellationToken cancellationToken);
        Task<Workspace> GetWorkspaceAsync(int id, CancellationToken cancellationToken);
        Task<WorkspaceUsersResponse> GetWorkspaceUsersAsync(int id, CancellationToken cancellationToken); //TODO change return type to WorkspaceUsers, avoid using Response objects in Application layer
    }
}
