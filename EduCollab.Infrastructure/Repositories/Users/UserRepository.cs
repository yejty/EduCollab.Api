using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Repositories.Users;
using EduCollab.Application.Services.Users;
using EduCollab.Contracts.Workspaces;
using EduCollab.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using System.Data.Common;

namespace EduCollab.Infrastructure.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPasswordHasher<PasswordHasherUser> _passwordHasher;

        public UserRepository(IDbConnectionFactory dbConnectionFactory, IPasswordHasher<PasswordHasherUser> passwordHasher)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _passwordHasher = passwordHasher;
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
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

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

        public async Task RevokeActiveLoginCodesForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE UserLoginCodes
                SET UsedAt = @UsedAt
                WHERE UserId = @UserId AND UsedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, UsedAt = revokedAtUtc }, cancellationToken: cancellationToken));
        }

        public async Task InsertLoginCodeAsync(int userId, string codeHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO UserLoginCodes (UserId, CodeHash, ExpiresAt, CreatedAt)
                VALUES (@UserId, @CodeHash, @ExpiresAt, @CreatedAt);
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId,
                        CodeHash = codeHashSha256Hex,
                        ExpiresAt = expiresAtUtc,
                        CreatedAt = createdAtUtc
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<LoginCodeConsumeResult> ConsumeLoginCodeAsync(string email, string codeHashSha256Hex, DateTimeOffset utcNow, int maxAttempts, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var row = await connection.QuerySingleOrDefaultAsync<LoginCodeRow>(
                new CommandDefinition(
                    """
                    SELECT lc.Id, lc.UserId, lc.CodeHash, lc.FailedAttempts
                    FROM UserLoginCodes lc
                    INNER JOIN Users u ON u.Id = lc.UserId
                    WHERE LOWER(u.Email) = LOWER(@Email)
                      AND u.EmailConfirmedAtUtc IS NOT NULL
                      AND lc.UsedAt IS NULL
                      AND lc.ExpiresAt > @Now
                    ORDER BY lc.Id DESC
                    LIMIT 1
                    FOR UPDATE
                    """,
                    new { Email = email, Now = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (row is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return new LoginCodeConsumeResult();
            }

            if (!string.Equals(row.CodeHash, codeHashSha256Hex, StringComparison.Ordinal))
            {
                var nextAttemptCount = row.FailedAttempts + 1;
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE UserLoginCodes
                        SET FailedAttempts = @FailedAttempts,
                            UsedAt = CASE WHEN @FailedAttempts >= @MaxAttempts THEN @Now ELSE UsedAt END
                        WHERE Id = @Id;
                        """,
                        new
                        {
                            FailedAttempts = nextAttemptCount,
                            MaxAttempts = maxAttempts,
                            Now = utcNow,
                            row.Id
                        },
                        transaction: tx,
                        cancellationToken: cancellationToken));

                await tx.CommitAsync(cancellationToken);
                return new LoginCodeConsumeResult
                {
                    IsLocked = nextAttemptCount >= maxAttempts,
                    RemainingAttempts = Math.Max(0, maxAttempts - nextAttemptCount)
                };
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE UserLoginCodes SET UsedAt = @Now WHERE Id = @Id;",
                    new { Now = utcNow, row.Id },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return new LoginCodeConsumeResult
            {
                UserId = row.UserId
            };
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
                SELECT Id, Email, PasswordHash, EmailConfirmedAtUtc
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
                SELECT Id, Email, PasswordHash, EmailConfirmedAtUtc
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

        public async Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, DateTime? EmailConfirmedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO Users (FirstName, LastName, Email, PasswordHash, EmailConfirmedAtUtc)
                VALUES (@FirstName, @LastName, @Email, @PasswordHash, @EmailConfirmedAtUtc)
                RETURNING Id;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    sql,
                    new { FirstName = firstName, LastName = lastName, Email = email, PasswordHash = passwordHash, EmailConfirmedAtUtc },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var result = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Users
                    SET FirstName = @FirstName,
                        LastName = @LastName
                    WHERE Id = @Id;
                    """,
                    new
                    {
                        user.FirstName,
                        user.LastName,
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

        public async Task<Workspace?> GetWorkspaceByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<Workspace>(
                 new CommandDefinition(
                     """
                    SELECT
                        Id,
                        Name,
                        Description,
                        CreatedAtUtc,
                        UpdatedAtUtc,
                        COALESCE(CreatedByUserId, 0) AS CreatedByUserId,
                        IsArchived
                    FROM Workspaces
                    WHERE Id = @Id
                      AND IsArchived = FALSE
                    LIMIT 1;
                    """,
                     new { Id = id },
                     cancellationToken: cancellationToken));
        }

        public async Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<WorkspaceMember>(
                new CommandDefinition(
                    """
                    SELECT
                        WorkspaceId,
                        UserId,
                        Role,
                        JoinedAtUtc
                    FROM WorkspaceMembers
                    WHERE WorkspaceId = @WorkspaceId
                      AND UserId = @UserId
                    LIMIT 1;
                    """,
                    new { UserId = userId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var members = await connection.QueryAsync<WorkspaceMember>(
                new CommandDefinition(
                    """
                    SELECT
                        WorkspaceId,
                        UserId,
                        Role,
                        JoinedAtUtc
                    FROM WorkspaceMembers
                    WHERE WorkspaceId = @WorkspaceId
                    ORDER BY JoinedAtUtc, UserId;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return members.AsList();
        }

        public async Task<int> CreateWorkspaceWithOwnerAsync(Workspace workspace, int ownerUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var workspaceId = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO Workspaces (Name, Description, CreatedAtUtc, UpdatedAtUtc, CreatedByUserId, IsArchived)
                    VALUES (@Name, @Description, @CreatedAtUtc, @UpdatedAtUtc, @CreatedByUserId, @IsArchived)
                    RETURNING Id;
                    """,
                    new
                    {
                        workspace.Name,
                        workspace.Description,
                        workspace.CreatedAtUtc,
                        workspace.UpdatedAtUtc,
                        workspace.CreatedByUserId,
                        workspace.IsArchived
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, JoinedAtUtc)
                    VALUES (@WorkspaceId, @UserId, @Role, @JoinedAtUtc);
                    """,
                    new { WorkspaceId = workspaceId, UserId = ownerUserId, Role = WorkspaceRole.Owner, JoinedAtUtc = now },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Users SET WorkspaceId = @WorkspaceId WHERE Id = @UserId;",
                    new { WorkspaceId = workspaceId, UserId = ownerUserId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return workspaceId;
        }

        public async Task<bool> IsUserInAnyWorkspaceAsync(int userId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT EXISTS(SELECT 1 FROM WorkspaceMembers WHERE UserId = @UserId);
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        }

        public async Task<bool> IsUserWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT EXISTS(
                    SELECT 1 FROM WorkspaceMembers
                    WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId);
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { WorkspaceId = workspaceId, UserId = userId }, cancellationToken: cancellationToken));
        }

        public async Task<bool> IsEmailMemberOfWorkspaceAsync(int workspaceId, string email, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT EXISTS(
                    SELECT 1
                    FROM WorkspaceMembers wm
                    INNER JOIN Users u ON u.Id = wm.UserId
                    WHERE wm.WorkspaceId = @WorkspaceId
                      AND LOWER(u.Email) = LOWER(@Email));
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { WorkspaceId = workspaceId, Email = email }, cancellationToken: cancellationToken));
        }

        public async Task RevokePendingWorkspaceInvitationsAsync(int workspaceId, string email, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE WorkspaceInvitations
                SET UsedAt = @UsedAt
                WHERE WorkspaceId = @WorkspaceId
                  AND LOWER(Email) = LOWER(@Email)
                  AND UsedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { WorkspaceId = workspaceId, Email = email, UsedAt = revokedAtUtc }, cancellationToken: cancellationToken));
        }

        public async Task InsertWorkspaceInvitationAsync(
            int workspaceId,
            string email,
            string tokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            int invitedByUserId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO WorkspaceInvitations (WorkspaceId, Email, TokenHash, ExpiresAt, CreatedAt, InvitedByUserId)
                VALUES (@WorkspaceId, @Email, @TokenHash, @ExpiresAt, @CreatedAt, @InvitedByUserId);
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        WorkspaceId = workspaceId,
                        Email = email,
                        TokenHash = tokenHashSha256Hex,
                        ExpiresAt = expiresAtUtc,
                        CreatedAt = createdAtUtc,
                        InvitedByUserId = invitedByUserId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<int?> AcceptWorkspaceInvitationAndRegisterUserAsync(
            int workspaceId,
            string tokenHashSha256Hex,
            string email,
            string firstName,
            string lastName,
            string plainPassword,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var invitationRow = await connection.QuerySingleOrDefaultAsync<LockedInvitationRow>(
                new CommandDefinition(
                    """
                    SELECT wi.Id, wi.Email
                    FROM WorkspaceInvitations wi
                    WHERE wi.WorkspaceId = @WorkspaceId
                      AND wi.TokenHash = @TokenHash
                      AND wi.UsedAt IS NULL
                      AND wi.ExpiresAt > @Now
                    ORDER BY wi.Id DESC
                    LIMIT 1
                    FOR UPDATE
                    """,
                    new { WorkspaceId = workspaceId, TokenHash = tokenHashSha256Hex, Now = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (invitationRow is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(invitationRow.Email.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            var existingUserId = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT Id
                    FROM Users
                    WHERE LOWER(Email) = LOWER(@Email)
                    LIMIT 1;
                    """,
                    new { Email = email },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (existingUserId is not null)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            var hashingUser = new PasswordHasherUser { Id = email };
            var initialHash = _passwordHasher.HashPassword(hashingUser, plainPassword);

            var userId = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, EmailConfirmedAtUtc)
                    VALUES (@FirstName, @LastName, @Email, @PasswordHash, @EmailConfirmedAtUtc)
                    RETURNING Id;
                    """,
                    new { FirstName = firstName, LastName = lastName, Email = email, PasswordHash = initialHash, EmailConfirmedAtUtc = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            var hashingUserWithId = new PasswordHasherUser { Id = userId.ToString() };
            var finalHash = _passwordHasher.HashPassword(hashingUserWithId, plainPassword);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @UserId;",
                    new { PasswordHash = finalHash, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, JoinedAtUtc)
                    VALUES (@WorkspaceId, @UserId, @Role, @JoinedAtUtc);
                    """,
                    new { WorkspaceId = workspaceId, UserId = userId, Role = WorkspaceRole.Member, JoinedAtUtc = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Users SET WorkspaceId = @WorkspaceId WHERE Id = @UserId;",
                    new { WorkspaceId = workspaceId, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE WorkspaceInvitations SET UsedAt = @Now WHERE Id = @Id;",
                    new { Now = utcNow, Id = invitationRow.Id },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return userId;
        }

        public async Task<bool> RemoveWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM WorkspaceMembers
                    WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId;
                    """,
                    new { WorkspaceId = workspaceId, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (deleted == 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return false;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Users
                    SET WorkspaceId = NULL
                    WHERE Id = @UserId AND WorkspaceId = @WorkspaceId;
                    """,
                    new { UserId = userId, WorkspaceId = workspaceId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<Workspace?> UpdateWorkspaceAsync(Workspace workspace, int userId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<Workspace>(
                new CommandDefinition(
                    """
                    UPDATE Workspaces
                    SET Name = @Name,
                        Description = @Description,
                        UpdatedAtUtc = @UpdatedAtUtc,
                        IsArchived = @IsArchived
                    WHERE Id = @Id
                      AND EXISTS (
                          SELECT 1 FROM WorkspaceMembers wm
                          WHERE wm.WorkspaceId = @Id AND wm.UserId = @UserId
                      )
                    RETURNING
                        Id,
                        Name,
                        Description,
                        CreatedAtUtc,
                        UpdatedAtUtc,
                        COALESCE(CreatedByUserId, 0) AS CreatedByUserId,
                        IsArchived;
                    """,
                    new
                    {
                        workspace.Id,
                        workspace.Name,
                        workspace.Description,
                        workspace.UpdatedAtUtc,
                        workspace.IsArchived,
                        UserId = userId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> SoftDeleteWorkspaceAsync(int workspaceId, int userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var archived = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Workspaces
                    SET IsArchived = TRUE,
                        UpdatedAtUtc = @Now
                    WHERE Id = @WorkspaceId
                      AND IsArchived = FALSE
                      AND EXISTS (
                          SELECT 1
                          FROM WorkspaceMembers wm
                          WHERE wm.WorkspaceId = @WorkspaceId AND wm.UserId = @UserId
                      );
                    """,
                    new { WorkspaceId = workspaceId, UserId = userId, Now = utcNow.UtcDateTime },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (archived == 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return false;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Users
                    SET WorkspaceId = NULL
                    WHERE WorkspaceId = @WorkspaceId;
                    """,
                    new { WorkspaceId = workspaceId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM WorkspaceMembers
                    WHERE WorkspaceId = @WorkspaceId;
                    """,
                    new { WorkspaceId = workspaceId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceInvitations
                    SET UsedAt = @Now
                    WHERE WorkspaceId = @WorkspaceId
                      AND UsedAt IS NULL;
                    """,
                    new { WorkspaceId = workspaceId, Now = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(member);

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<WorkspaceMember>(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceMembers
                    SET Role = @Role
                    WHERE WorkspaceId = @WorkspaceId
                      AND UserId = @UserId
                    RETURNING WorkspaceId, UserId, Role, JoinedAtUtc;
                    """,
                    new { WorkspaceId = id, UserId = userId, member.Role },
                    cancellationToken: cancellationToken));
        }

        public async Task RevokeActiveEmailConfirmationTokensForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE UserEmailConfirmationTokens
                SET UsedAt = @UsedAt
                WHERE UserId = @UserId AND UsedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, UsedAt = revokedAtUtc }, cancellationToken: cancellationToken));
        }

        public async Task InsertEmailConfirmationTokenAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO UserEmailConfirmationTokens (UserId, TokenHash, ExpiresAt, CreatedAt)
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

        public async Task<int?> GetUserIdForActiveEmailConfirmationTokenAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT t.UserId
                FROM UserEmailConfirmationTokens t
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

        public async Task<int?> ConfirmEmailAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var tokenId = await connection.QuerySingleOrDefaultAsync<long?>(
                new CommandDefinition(
                    """
                    SELECT t.Id
                    FROM UserEmailConfirmationTokens t
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
                    "SELECT UserId FROM UserEmailConfirmationTokens WHERE Id = @Id;",
                    new { Id = tokenId.Value },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE UserEmailConfirmationTokens SET UsedAt = @Now WHERE Id = @Id;",
                    new { Now = utcNow, Id = tokenId.Value },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Users
                    SET EmailConfirmedAtUtc = @Now
                    WHERE Id = @UserId;
                    """,
                    new { Now = utcNow, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return userId;
        }

        private sealed class LockedInvitationRow
        {
            public long Id { get; set; }
            public string Email { get; set; } = "";
        }

        private sealed class LoginCodeRow
        {
            public long Id { get; set; }
            public int UserId { get; set; }
            public string CodeHash { get; set; } = "";
            public int FailedAttempts { get; set; }
        }
    }
}
