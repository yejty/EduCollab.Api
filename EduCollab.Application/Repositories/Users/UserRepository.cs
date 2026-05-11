using Dapper;
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
            return await connection.QuerySingleOrDefaultAsync<UserCredentialRecordDto>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        }

        public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(
                    """
                    SELECT Id, FirstName, LastName, Email
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
    }
}
