namespace EduCollab.Application.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task InsertAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken);

        Task<StoredRefreshTokenRecord?> GetActiveByHashAsync(string tokenHashSha256Hex, CancellationToken cancellationToken);

        Task RevokeByIdAsync(long id, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);
    }
}
