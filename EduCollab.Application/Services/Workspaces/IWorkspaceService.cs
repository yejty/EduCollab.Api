using EduCollab.Application.Models.Users;
using EduCollab.Application.Models.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<bool> CreateUserInWorkspaceAsync(int workspaceId, User user, string password, string invitationToken, CancellationToken cancellationToken);
        Task InviteUserToWorkspaceAsync(int workspaceId, string email, CancellationToken cancellationToken);
        Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken);

        /// <summary>
        /// All workspaces (no membership filter).
        /// </summary>
        Task<List<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken);
        Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int id, CancellationToken cancellationToken);
        Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);

        /// <summary>
        /// When the current user is resolved and is a member of the workspace, returns their membership; otherwise null.
        /// </summary>
        Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(int workspaceId, CancellationToken cancellationToken);

        Task<bool> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken);
        Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken);
        Task<bool> DeleteWorkspaceAsync(int workspaceId, CancellationToken cancellationToken);
        Task RemoveWorkspaceMemberAsync(int workspaceId, int targetUserId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken);
    }
}
