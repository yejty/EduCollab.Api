using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Repositories.Users
{
    public interface IUserRepository
    {
        Task CreateAsync(User user, string password, string invitationToken, CancellationToken cancellationToken);

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
        Task InviteAsync(string email, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(User user, CancellationToken cancellationToken);
        Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken);
        Task<Workspace?> GetWorkspaceByIdAsync(int id, CancellationToken cancellationToken);
        Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int workspaceId, CancellationToken cancellationToken);
        Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
    }
}
