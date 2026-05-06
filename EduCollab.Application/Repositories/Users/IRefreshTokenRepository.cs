namespace EduCollab.Application.Repositories.Users
{
    public interface IRefreshTokenRepository
    {
        Task InsertAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);

        Task<StoredRefreshTokenRecord?> GetActiveByHashAsync(string tokenHashSha256Hex, CancellationToken cancellationToken);

        Task RevokeByIdAsync(long id, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);
    }
}
