using Dapper;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public RefreshTokenRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task InsertAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO UserRefreshTokens (UserId, TokenHash, ExpiresAt, CreatedAt)
                VALUES (@UserId, @TokenHash, @ExpiresAt, @CreatedAt);
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, TokenHash = tokenHashSha256Hex, ExpiresAt = expiresAtUtc, CreatedAt = createdAtUtc }, cancellationToken: cancellationToken));
        }

        public async Task<StoredRefreshTokenRecord?> GetActiveByHashAsync(string tokenHashSha256Hex, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT Id, UserId
                FROM UserRefreshTokens
                WHERE TokenHash = @TokenHash
                  AND RevokedAt IS NULL
                  AND ExpiresAt > NOW()
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<StoredRefreshTokenRecord>(
                new CommandDefinition(sql, new { TokenHash = tokenHashSha256Hex }, cancellationToken: cancellationToken));
        }

        public async Task RevokeByIdAsync(long id, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE UserRefreshTokens
                SET RevokedAt = @RevokedAt
                WHERE Id = @Id AND RevokedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id, RevokedAt = revokedAtUtc }, cancellationToken: cancellationToken));
        }
    }
}
