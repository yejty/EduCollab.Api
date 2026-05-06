using Dapper;
using EduCollab.Application.Database;

namespace EduCollab.Application.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<UserCredentialRecord?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT Id, Email, PasswordHash
                FROM Users
                WHERE LOWER(Email) = LOWER(@Email)
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<UserCredentialRecord>(
                new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        }

        public async Task<UserCredentialRecord?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT Id, Email, PasswordHash
                FROM Users
                WHERE Id = @UserId
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<UserCredentialRecord>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
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
    }
}
