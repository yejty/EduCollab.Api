using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Repositories.Users
{
    public interface IUserRepository
    {
        /// <summary>
        /// Marks unused password reset tokens for the user as used so only the latest flow applies.
        /// </summary>
        Task RevokeActivePasswordResetTokensForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);

        Task InsertPasswordResetTokenAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken);

        /// <summary>
        /// Read-only: returns the user id when an unused, non-expired reset token exists for this email and hash.
        /// </summary>
        Task<int?> GetUserIdForActivePasswordResetTokenAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken);

        Task<int?> CompletePasswordResetAsync(string email, string tokenHashSha256Hex, string newPasswordHash, DateTimeOffset utcNow, CancellationToken cancellationToken);

        Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken);
        Task<UserCredentialRecordDto?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken);
        Task<UserCredentialRecordDto?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken);
        Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken);
        Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(User user, CancellationToken cancellationToken);
        Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken);
        Task<Workspace?> GetWorkspaceByIdAsync(int id, CancellationToken cancellationToken);
        Task<Workspace?> UpdateWorkspaceAsync(Workspace workspace, int userId, CancellationToken cancellationToken);
        Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int workspaceId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
        Task<int> CreateWorkspaceWithOwnerAsync(Workspace workspace, int ownerUserId, DateTimeOffset now, CancellationToken cancellationToken);

        Task<bool> IsUserInAnyWorkspaceAsync(int userId, CancellationToken cancellationToken);

        Task<bool> IsUserWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);

        Task<bool> IsEmailMemberOfWorkspaceAsync(int workspaceId, string email, CancellationToken cancellationToken);

        Task RevokePendingWorkspaceInvitationsAsync(int workspaceId, string email, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);

        Task InsertWorkspaceInvitationAsync(
            int workspaceId,
            string email,
            string tokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            int invitedByUserId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Validates token, creates user with password, adds workspace membership, sets WorkspaceId, consumes invitation. Returns null if invalid.
        /// </summary>
        Task<int?> AcceptWorkspaceInvitationAndRegisterUserAsync(
            int workspaceId,
            string tokenHashSha256Hex,
            string email,
            string firstName,
            string lastName,
            string plainPassword,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes the user from the workspace (membership row and clears <c>Users.WorkspaceId</c> when it matches). Returns false if the user was not a member.
        /// </summary>
        Task<bool> RemoveWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken);
    }
}
