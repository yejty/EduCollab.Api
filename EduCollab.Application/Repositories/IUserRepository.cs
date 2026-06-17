using EduCollab.Application.Models;
using EduCollab.Application.Services.Users;

namespace EduCollab.Application.Repositories
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
        Task RevokeActiveLoginCodesForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);
        Task InsertLoginCodeAsync(int userId, string codeHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken);
        Task<LoginCodeConsumeResult> ConsumeLoginCodeAsync(string email, string codeHashSha256Hex, DateTimeOffset utcNow, int maxAttempts, CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken);
        Task<UserCredentialRecordDto?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken);
        Task<UserCredentialRecordDto?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken);
        Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> IsPlatformAdminAsync(int userId, CancellationToken cancellationToken);
        Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, DateTime? EmailConfirmedAtUtc, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(User user, CancellationToken cancellationToken);
        Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken);

        Task RevokeActiveEmailConfirmationTokensForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);

        Task InsertEmailConfirmationTokenAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken);

        Task<int?> GetUserIdForActiveEmailConfirmationTokenAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken);

        Task<int?> ConfirmEmailAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken);
    }
}
