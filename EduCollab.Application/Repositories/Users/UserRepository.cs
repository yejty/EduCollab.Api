using Dapper;
using System.Data.Common;
using EduCollab.Application.Database;
using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task RevokeActivePasswordResetTokensForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE UserPasswordResetTokens
                SET UsedAt = @UsedAt
                WHERE UserId = @UserId AND UsedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, UsedAt = revokedAtUtc }, cancellationToken: cancellationToken));
        }

        public async Task InsertPasswordResetTokenAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO UserPasswordResetTokens (UserId, TokenHash, ExpiresAt, CreatedAt)
                VALUES (@UserId, @TokenHash, @ExpiresAt, @CreatedAt);
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId,
                        TokenHash = tokenHashSha256Hex,
                        ExpiresAt = expiresAtUtc,
                        CreatedAt = createdAtUtc
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<int?> GetUserIdForActivePasswordResetTokenAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT t.UserId
                FROM UserPasswordResetTokens t
                INNER JOIN Users u ON u.Id = t.UserId
                WHERE LOWER(u.Email) = LOWER(@Email)
                  AND t.TokenHash = @TokenHash
                  AND t.UsedAt IS NULL
                  AND t.ExpiresAt > @Now
                ORDER BY t.Id DESC
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(sql, new { Email = email, TokenHash = tokenHashSha256Hex, Now = utcNow }, cancellationToken: cancellationToken));
        }

        public async Task<int?> CompletePasswordResetAsync(string email, string tokenHashSha256Hex, string newPasswordHash, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
                throw new InvalidOperationException("Database connection must support transactions.");

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var tokenId = await connection.QuerySingleOrDefaultAsync<long?>(
                new CommandDefinition(
                    """
                    SELECT t.Id
                    FROM UserPasswordResetTokens t
                    INNER JOIN Users u ON u.Id = t.UserId
                    WHERE LOWER(u.Email) = LOWER(@Email)
                      AND t.TokenHash = @TokenHash
                      AND t.UsedAt IS NULL
                      AND t.ExpiresAt > @Now
                    ORDER BY t.Id DESC
                    LIMIT 1
                    FOR UPDATE
                    """,
                    new { Email = email, TokenHash = tokenHashSha256Hex, Now = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (tokenId is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            var userId = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    "SELECT UserId FROM UserPasswordResetTokens WHERE Id = @Id;",
                    new { Id = tokenId.Value },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE UserPasswordResetTokens SET UsedAt = @Now WHERE Id = @Id;",
                    new { Now = utcNow, Id = tokenId.Value },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @UserId;",
                    new { PasswordHash = newPasswordHash, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return userId;
        }

        public async Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE Users
                SET PasswordHash = @PasswordHash
                WHERE Id = @UserId;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, PasswordHash = passwordHash }, cancellationToken: cancellationToken));
        }

        public Task CreateAsync(User user, string password, string invitationToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    "SELECT EXISTS(SELECT 1 FROM Users WHERE Id = @Id);",
                    new { Id = id },
                    cancellationToken: cancellationToken));
        }

        public async Task<UserCredentialRecordDto?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT Id, Email, PasswordHash
                FROM Users
                WHERE LOWER(Email) = LOWER(@Email)
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<UserCredentialRecordDto>(
                new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        }

        public async Task<UserCredentialRecordDto?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT Id, Email, PasswordHash
                FROM Users
                WHERE Id = @UserId
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var record = await connection.QuerySingleOrDefaultAsync<UserCredentialRecordDto>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
            if (record is null)
            {
                return null;
            }
            return record;
        }

        public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(
                    """
                    SELECT Id, FirstName, LastName, Email, WorkspaceId
                    FROM Users
                    WHERE Id = @Id
                    LIMIT 1;
                    """,
                    new { Id = id },
                    cancellationToken: cancellationToken));
        }

        public async Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO Users (FirstName, LastName, Email, PasswordHash)
                VALUES (@FirstName, @LastName, @Email, @PasswordHash)
                RETURNING Id;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleAsync<int>(
                new CommandDefinition(sql, new { FirstName = firstName, LastName = lastName, Email = email, PasswordHash = passwordHash }, cancellationToken: cancellationToken));
        }

        public Task InviteAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var result = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Users
                    SET FirstName = @FirstName,
                        LastName = @LastName,
                        Email = @Email
                    WHERE Id = @Id;
                    """,
                    new
                    {
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.Id
                    },
                    cancellationToken: cancellationToken));
            return result > 0;
        }

        public async Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var result = await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM Users WHERE Id = @Id;", new { Id = id }, cancellationToken: cancellationToken));
            return result > 0;
        }
    }
}
