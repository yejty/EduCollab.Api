using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;
using System.Data.Common;

namespace EduCollab.Infrastructure.Repositories
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public GroupRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task<int> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Database connection must support transactions.");
            }

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var groupId = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO Groups (WorkspaceId, Name, Description, CreatedAtUtc, UpdatedAtUtc, CreatedByUserId, UserCount)
                    VALUES (@WorkspaceId, @Name, @Description, @CreatedAtUtc, @UpdatedAtUtc, @CreatedByUserId, @UserCount)
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        group.Name,
                        group.Description,
                        group.CreatedAtUtc,
                        group.UpdatedAtUtc,
                        group.CreatedByUserId,
                        group.UserCount
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO GroupMembers (GroupId, UserId, Role, JoinedAtUtc)
                    VALUES (@GroupId, @UserId, @Role, @JoinedAtUtc);
                    """,
                    new
                    {
                        GroupId = groupId,
                        UserId = group.CreatedByUserId,
                        Role = GroupRole.Admin.ToString(),
                        JoinedAtUtc = group.CreatedAtUtc,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return groupId;
        }

        public async Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM Groups
                    WHERE Id = @GroupId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new { GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var groups = await connection.QueryAsync<Group>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        Name,
                        Description,
                        CreatedAtUtc,
                        UpdatedAtUtc,
                        CreatedByUserId,
                        UserCount
                    FROM Groups
                    WHERE WorkspaceId = @WorkspaceId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return groups.AsList();
        }

        public async Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<Group>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        Name,
                        Description,
                        CreatedAtUtc,
                        UpdatedAtUtc,
                        CreatedByUserId,
                        UserCount
                    FROM Groups
                    WHERE Id = @GroupId
                      AND WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken)
        {
            var existing = await GetGroupByIdAsync(workspaceId, group.Id, cancellationToken);
            if (existing is null)
                return null;

            var updatedName = string.IsNullOrWhiteSpace(group.Name) ? existing.Name : group.Name.Trim();
            var updatedDescription = group.Description ?? existing.Description;
            var updatedAtUtc = DateTime.UtcNow;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<Group>(
                new CommandDefinition(
                    """
                    UPDATE Groups
                    SET Name = @Name,
                        Description = @Description,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @Id
                      AND WorkspaceId = @WorkspaceId
                    RETURNING
                        Id,
                        Name,
                        Description,
                        CreatedAtUtc,
                        UpdatedAtUtc,
                        CreatedByUserId,
                        UserCount;
                    """,
                    new
                    {
                        group.Id,
                        WorkspaceId = workspaceId,
                        Name = updatedName,
                        Description = updatedDescription,
                        UpdatedAtUtc = updatedAtUtc,
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<List<GroupMember>> GetAllGroupMembersAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var members = await connection.QueryAsync<GroupMember>(
                new CommandDefinition(
                    """
                    SELECT
                        gm.GroupId,
                        gm.UserId,
                        gm.Role,
                        gm.JoinedAtUtc
                    FROM GroupMembers gm
                    INNER JOIN Groups g ON g.Id = gm.GroupId
                    WHERE gm.GroupId = @GroupId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY gm.JoinedAtUtc, gm.UserId;
                    """,
                    new { GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return members.AsList();
        }

        public async Task<GroupMember?> GetGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<GroupMember>(
                new CommandDefinition(
                    """
                    SELECT
                        gm.GroupId,
                        gm.UserId,
                        gm.Role,
                        gm.JoinedAtUtc
                    FROM GroupMembers gm
                    INNER JOIN Groups g ON g.Id = gm.GroupId
                    WHERE gm.GroupId = @GroupId
                      AND gm.UserId = @UserId
                      AND g.WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { GroupId = groupId, UserId = userId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<GroupMember?> CreateGroupMemberAsync(int workspaceId, GroupMember member, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var created = await connection.QuerySingleOrDefaultAsync<GroupMember>(
                new CommandDefinition(
                    """
                    INSERT INTO GroupMembers (GroupId, UserId, Role, JoinedAtUtc)
                    SELECT @GroupId, @UserId, @Role, @JoinedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (GroupId, UserId) DO NOTHING
                    RETURNING GroupId, UserId, Role, JoinedAtUtc;
                    """,
                    new
                    {
                        member.GroupId,
                        member.UserId,
                        Role = member.Role.ToString(),
                        member.JoinedAtUtc,
                        WorkspaceId = workspaceId,
                    },
                    cancellationToken: cancellationToken));

            if (created is not null)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE Groups
                        SET UserCount = (
                            SELECT COUNT(*)
                            FROM GroupMembers
                            WHERE GroupId = @GroupId
                        )
                        WHERE Id = @GroupId
                          AND WorkspaceId = @WorkspaceId;
                        """,
                        new { member.GroupId, WorkspaceId = workspaceId },
                        cancellationToken: cancellationToken));
            }

            return created;
        }

        public async Task<GroupMember?> UpdateGroupMemberAsync(int workspaceId, int groupId, int userId, GroupRole role, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var updated = await connection.QuerySingleOrDefaultAsync<GroupMember>(
                new CommandDefinition(
                    """
                    UPDATE GroupMembers gm
                    SET Role = @Role
                    FROM Groups g
                    WHERE gm.GroupId = @GroupId
                      AND gm.UserId = @UserId
                      AND g.Id = gm.GroupId
                      AND g.WorkspaceId = @WorkspaceId
                    RETURNING gm.GroupId, gm.UserId, gm.Role, gm.JoinedAtUtc;
                    """,
                    new { GroupId = groupId, UserId = userId, WorkspaceId = workspaceId, Role = role.ToString() },
                    cancellationToken: cancellationToken));

            return updated;
        }

        public async Task<bool> DeleteGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken)
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
                    DELETE FROM GroupMembers gm
                    USING Groups g
                    WHERE gm.GroupId = @GroupId
                      AND gm.UserId = @UserId
                      AND g.Id = gm.GroupId
                      AND g.WorkspaceId = @WorkspaceId;
                    """,
                    new { GroupId = groupId, UserId = userId, WorkspaceId = workspaceId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (deleted > 0)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE Groups
                        SET UserCount = (
                            SELECT COUNT(*)
                            FROM GroupMembers
                            WHERE GroupId = @GroupId
                        )
                        WHERE Id = @GroupId
                          AND WorkspaceId = @WorkspaceId;
                        """,
                        new { GroupId = groupId, WorkspaceId = workspaceId },
                        transaction: tx,
                        cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
            return deleted > 0;
        }
    }
}
