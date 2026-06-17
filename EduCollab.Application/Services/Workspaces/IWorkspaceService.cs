using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<bool> CreateUserInWorkspaceAsync(int workspaceId, User user, string password, string invitationToken, CancellationToken cancellationToken);
        Task<bool> CreateUserFromInvitationAsync(User user, string password, string invitationToken, CancellationToken cancellationToken);
        Task InviteUserToWorkspaceAsync(int workspaceId, string email, WorkspaceRole role, CancellationToken cancellationToken);
        Task InviteUserToCurrentWorkspaceAsync(string email, WorkspaceRole role, CancellationToken cancellationToken);
        Task<WorkspaceMember?> JoinWorkspaceFromInvitationAsync(string invitationToken, CancellationToken cancellationToken);
        Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken);
        Task<Workspace?> GetCurrentWorkspaceAsync(CancellationToken cancellationToken);

        /// <summary>
        /// All workspaces (no membership filter).
        /// </summary>
        Task<List<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken);
        Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int id, CancellationToken cancellationToken);
        Task<List<WorkspaceMember>> GetCurrentWorkspaceMembersAsync(CancellationToken cancellationToken);
        Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> GetCurrentWorkspaceMemberAsync(int userId, CancellationToken cancellationToken);

        /// <summary>
        /// When the current user is resolved and is a member of the workspace, returns their membership; otherwise null.
        /// </summary>
        Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(int workspaceId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(CancellationToken cancellationToken);

        Task<bool> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken);
        Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken);
        Task<bool> UpdateCurrentWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken);
        Task<bool> DeleteWorkspaceAsync(int workspaceId, CancellationToken cancellationToken);
        Task<bool> DeleteCurrentWorkspaceAsync(CancellationToken cancellationToken);
        Task RemoveWorkspaceMemberAsync(int workspaceId, int targetUserId, CancellationToken cancellationToken);
        Task RemoveCurrentWorkspaceMemberAsync(int targetUserId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken);
        Task<WorkspaceMember?> UpdateCurrentWorkspaceMemberAsync(int userId, WorkspaceMember member, CancellationToken cancellationToken);
    }
}
