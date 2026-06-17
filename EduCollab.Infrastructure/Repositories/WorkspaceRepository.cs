using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Users;
using EduCollab.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using System.Data.Common;

namespace EduCollab.Infrastructure.Repositories
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPasswordHasher<PasswordHasherUser> _passwordHasher;

        public WorkspaceRepository(IDbConnectionFactory dbConnectionFactory, IPasswordHasher<PasswordHasherUser> passwordHasher)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _passwordHasher = passwordHasher;
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

        public async Task<List<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var workspaces = await connection.QueryAsync<Workspace>(
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
                    ORDER BY Name ASC, Id ASC;
                    """,
                    cancellationToken: cancellationToken));

            return workspaces.AsList();
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
            WorkspaceRole role,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            int invitedByUserId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO WorkspaceInvitations (WorkspaceId, Email, TokenHash, Role, ExpiresAt, CreatedAt, InvitedByUserId)
                VALUES (@WorkspaceId, @Email, @TokenHash, @Role, @ExpiresAt, @CreatedAt, @InvitedByUserId);
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
                        Role = role.ToPersistedString(),
                        ExpiresAt = expiresAtUtc,
                        CreatedAt = createdAtUtc,
                        InvitedByUserId = invitedByUserId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<WorkspaceInvitationDetails?> GetActiveWorkspaceInvitationAsync(
            string tokenHashSha256Hex,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT WorkspaceId, Email, Role
                FROM WorkspaceInvitations
                WHERE TokenHash = @TokenHash
                  AND UsedAt IS NULL
                  AND ExpiresAt > @Now
                ORDER BY Id DESC
                LIMIT 1;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var row = await connection.QuerySingleOrDefaultAsync<ActiveInvitationRow>(
                new CommandDefinition(sql, new { TokenHash = tokenHashSha256Hex, Now = utcNow }, cancellationToken: cancellationToken));

            if (row is null)
            {
                return null;
            }

            return new WorkspaceInvitationDetails
            {
                WorkspaceId = row.WorkspaceId,
                Email = row.Email,
                Role = WorkspaceRoleExtensions.FromPersistedOrViewer(row.Role),
            };
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
                    SELECT wi.Id, wi.Email, wi.Role
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

            var invitedRole = WorkspaceRoleExtensions.FromPersistedOrViewer(invitationRow.Role);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, JoinedAtUtc)
                    VALUES (@WorkspaceId, @UserId, @Role, @JoinedAtUtc);
                    """,
                    new { WorkspaceId = workspaceId, UserId = userId, Role = invitedRole, JoinedAtUtc = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (invitedRole == WorkspaceRole.Owner)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE WorkspaceMembers
                        SET Role = @ManagerRole
                        WHERE WorkspaceId = @WorkspaceId
                          AND UserId <> @UserId
                          AND Role = @OwnerRole;
                        """,
                        new
                        {
                            WorkspaceId = workspaceId,
                            UserId = userId,
                            OwnerRole = WorkspaceRole.Owner.ToPersistedString(),
                            ManagerRole = WorkspaceRole.Manager.ToPersistedString(),
                        },
                        transaction: tx,
                        cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Users SET WorkspaceId = @WorkspaceId WHERE Id = @UserId;",
                    new { WorkspaceId = workspaceId, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE WorkspaceInvitations SET UsedAt = @Now WHERE Id = @Id;",
                    new { Now = utcNow, invitationRow.Id },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return userId;
        }

        public async Task<WorkspaceMember?> AcceptWorkspaceInvitationForExistingUserAsync(
            int workspaceId,
            string tokenHashSha256Hex,
            int userId,
            string email,
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
                    SELECT wi.Id, wi.Email, wi.Role
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

            var alreadyMember = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT EXISTS(
                        SELECT 1
                        FROM WorkspaceMembers
                        WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId);
                    """,
                    new { WorkspaceId = workspaceId, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (alreadyMember)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            var inAnyWorkspace = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT EXISTS(SELECT 1 FROM WorkspaceMembers WHERE UserId = @UserId);
                    """,
                    new { UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (inAnyWorkspace)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            var invitedRole = WorkspaceRoleExtensions.FromPersistedOrViewer(invitationRow.Role);

            var member = await connection.QuerySingleAsync<WorkspaceMember>(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, JoinedAtUtc)
                    VALUES (@WorkspaceId, @UserId, @Role, @JoinedAtUtc)
                    RETURNING WorkspaceId, UserId, Role, JoinedAtUtc;
                    """,
                    new { WorkspaceId = workspaceId, UserId = userId, Role = invitedRole, JoinedAtUtc = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (invitedRole == WorkspaceRole.Owner)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE WorkspaceMembers
                        SET Role = @ManagerRole
                        WHERE WorkspaceId = @WorkspaceId
                          AND UserId <> @UserId
                          AND Role = @OwnerRole;
                        """,
                        new
                        {
                            WorkspaceId = workspaceId,
                            UserId = userId,
                            OwnerRole = WorkspaceRole.Owner.ToPersistedString(),
                            ManagerRole = WorkspaceRole.Manager.ToPersistedString(),
                        },
                        transaction: tx,
                        cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Users SET WorkspaceId = @WorkspaceId WHERE Id = @UserId;",
                    new { WorkspaceId = workspaceId, UserId = userId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE WorkspaceInvitations SET UsedAt = @Now WHERE Id = @Id;",
                    new { Now = utcNow, invitationRow.Id },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return member;
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

        public async Task DemoteWorkspaceOwnersExceptAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceMembers
                    SET Role = @ManagerRole
                    WHERE WorkspaceId = @WorkspaceId
                      AND UserId <> @UserId
                      AND Role = @OwnerRole;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        UserId = userId,
                        OwnerRole = WorkspaceRole.Owner.ToPersistedString(),
                        ManagerRole = WorkspaceRole.Manager.ToPersistedString(),
                    },
                    cancellationToken: cancellationToken));
        }

        private sealed class LockedInvitationRow
        {
            public long Id { get; set; }
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
        }

        private sealed class ActiveInvitationRow
        {
            public int WorkspaceId { get; set; }
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
        }
    }
}
