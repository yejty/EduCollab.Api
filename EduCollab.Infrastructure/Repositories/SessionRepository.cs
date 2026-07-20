using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public sealed class SessionRepository : ISessionRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string SessionSelectColumns =
            """
            s.Id,
            s.WorkspaceId,
            s.HostUserId,
            s.SceneId,
            s.FlowId,
            s.Status,
            s.IncludeAssets,
            s.AllowGuestLink,
            s.Name,
            s.Description,
            s.DefaultRole,
            s.CreatedAtUtc,
            s.StartedAtUtc,
            s.EndedAtUtc,
            u.FullName AS HostDisplayName,
            sc.Name AS SceneName,
            f.Name AS FlowName
            """;

        public SessionRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateSessionAsync(
            LiveSession session,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<int> userIds,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            var sessionId = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO LiveSessions (
                        WorkspaceId,
                        HostUserId,
                        SceneId,
                        FlowId,
                        Status,
                        IncludeAssets,
                        AllowGuestLink,
                        Name,
                        Description,
                        DefaultRole,
                        CreatedAtUtc)
                    VALUES (
                        @WorkspaceId,
                        @HostUserId,
                        @SceneId,
                        @FlowId,
                        @Status,
                        @IncludeAssets,
                        @AllowGuestLink,
                        @Name,
                        @Description,
                        @DefaultRole,
                        @CreatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        session.WorkspaceId,
                        session.HostUserId,
                        session.SceneId,
                        session.FlowId,
                        session.Status,
                        session.IncludeAssets,
                        session.AllowGuestLink,
                        session.Name,
                        session.Description,
                        session.DefaultRole,
                        session.CreatedAtUtc,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await InsertSharesAsync(connection, transaction, sessionId, groupIds, userIds, cancellationToken);
            transaction.Commit();
            return sessionId;
        }

        public async Task<LiveSession?> GetSessionByIdAsync(int workspaceId, int sessionId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var session = await connection.QuerySingleOrDefaultAsync<LiveSession>(
                new CommandDefinition(
                    $"""
                    SELECT {SessionSelectColumns}
                    FROM LiveSessions s
                    INNER JOIN Users u ON u.Id = s.HostUserId
                    LEFT JOIN Scenes sc ON sc.Id = s.SceneId
                    LEFT JOIN Flows f ON f.Id = s.FlowId
                    WHERE s.WorkspaceId = @WorkspaceId AND s.Id = @SessionId;
                    """,
                    new { WorkspaceId = workspaceId, SessionId = sessionId },
                    cancellationToken: cancellationToken));

            if (session is null)
                return null;

            await PopulateSharesAsync(connection, session, cancellationToken);
            return session;
        }

        public async Task<LiveSession?> GetSessionByIdAsync(int sessionId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var session = await connection.QuerySingleOrDefaultAsync<LiveSession>(
                new CommandDefinition(
                    $"""
                    SELECT {SessionSelectColumns}
                    FROM LiveSessions s
                    INNER JOIN Users u ON u.Id = s.HostUserId
                    LEFT JOIN Scenes sc ON sc.Id = s.SceneId
                    LEFT JOIN Flows f ON f.Id = s.FlowId
                    WHERE s.Id = @SessionId;
                    """,
                    new { SessionId = sessionId },
                    cancellationToken: cancellationToken));

            if (session is null)
                return null;

            await PopulateSharesAsync(connection, session, cancellationToken);
            return session;
        }

        public async Task<List<LiveSession>> ListSessionsAsync(
            int workspaceId,
            string? status,
            int? sharedWithUserId,
            IReadOnlyCollection<int> accessibleGroupIds,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var sessions = (await connection.QueryAsync<LiveSession>(
                new CommandDefinition(
                    $"""
                    SELECT DISTINCT {SessionSelectColumns}
                    FROM LiveSessions s
                    INNER JOIN Users u ON u.Id = s.HostUserId
                    LEFT JOIN Scenes sc ON sc.Id = s.SceneId
                    LEFT JOIN Flows f ON f.Id = s.FlowId
                    LEFT JOIN SessionGroupShares sgs ON sgs.SessionId = s.Id
                    LEFT JOIN SessionUserShares sus ON sus.SessionId = s.Id
                    WHERE s.WorkspaceId = @WorkspaceId
                      AND (@Status IS NULL OR s.Status = @Status)
                      AND (
                        @SharedWithUserId IS NULL
                        OR s.HostUserId = @SharedWithUserId
                        OR sus.UserId = @SharedWithUserId
                        OR sgs.GroupId = ANY(@AccessibleGroupIds)
                      )
                    ORDER BY s.CreatedAtUtc DESC;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        Status = status,
                        SharedWithUserId = sharedWithUserId,
                        AccessibleGroupIds = accessibleGroupIds.ToArray(),
                    },
                    cancellationToken: cancellationToken))).ToList();

            foreach (var session in sessions)
                await PopulateSharesAsync(connection, session, cancellationToken);

            return sessions;
        }

        public async Task<bool> EndSessionAsync(int workspaceId, int sessionId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE LiveSessions
                    SET Status = @Status,
                        EndedAtUtc = @EndedAtUtc
                    WHERE WorkspaceId = @WorkspaceId
                      AND Id = @SessionId
                      AND Status <> @Status;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        SessionId = sessionId,
                        Status = LiveSessionStatuses.Ended,
                        EndedAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken: cancellationToken));

            return rows > 0;
        }

        public async Task<bool> MarkRoomStartedAsync(int sessionId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE LiveSessions
                    SET Status = @ActiveStatus,
                        StartedAtUtc = COALESCE(StartedAtUtc, @StartedAtUtc),
                        EndedAtUtc = NULL
                    WHERE Id = @SessionId
                      AND Status <> @EndedStatus;
                    """,
                    new
                    {
                        SessionId = sessionId,
                        ActiveStatus = LiveSessionStatuses.Active,
                        EndedStatus = LiveSessionStatuses.Ended,
                        StartedAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken: cancellationToken));

            return rows > 0;
        }

        public async Task<bool> MarkRoomEndedAsync(int sessionId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE LiveSessions
                    SET Status = @Status,
                        EndedAtUtc = @EndedAtUtc
                    WHERE Id = @SessionId
                      AND Status <> @Status;
                    """,
                    new
                    {
                        SessionId = sessionId,
                        Status = LiveSessionStatuses.Ended,
                        EndedAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken: cancellationToken));

            return rows > 0;
        }

        public async Task ReplaceSessionSharesAsync(
            int sessionId,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<int> userIds,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM SessionGroupShares WHERE SessionId = @SessionId;",
                    new { SessionId = sessionId },
                    transaction,
                    cancellationToken: cancellationToken));
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM SessionUserShares WHERE SessionId = @SessionId;",
                    new { SessionId = sessionId },
                    transaction,
                    cancellationToken: cancellationToken));

            await InsertSharesAsync(connection, transaction, sessionId, groupIds, userIds, cancellationToken);
            transaction.Commit();
        }

        public async Task AddParticipantAsync(
            int sessionId,
            int? userId,
            string? guestId,
            string displayName,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO SessionParticipants (SessionId, UserId, GuestId, DisplayName, JoinedAtUtc)
                    VALUES (@SessionId, @UserId, @GuestId, @DisplayName, @JoinedAtUtc);
                    """,
                    new
                    {
                        SessionId = sessionId,
                        UserId = userId,
                        GuestId = guestId,
                        DisplayName = displayName,
                        JoinedAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<SessionGuestLink> UpsertGuestLinkAsync(SessionGuestLink link, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM SessionGuestLinks WHERE SessionId = @SessionId;",
                    new { link.SessionId },
                    transaction,
                    cancellationToken: cancellationToken));

            var id = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO SessionGuestLinks (
                        SessionId,
                        TokenHash,
                        TokenPlaintext,
                        ExpiresAtUtc,
                        MaxUses,
                        UseCount,
                        CreatedAtUtc)
                    VALUES (
                        @SessionId,
                        @TokenHash,
                        @TokenPlaintext,
                        @ExpiresAtUtc,
                        @MaxUses,
                        @UseCount,
                        @CreatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        link.SessionId,
                        link.TokenHash,
                        link.TokenPlaintext,
                        link.ExpiresAtUtc,
                        link.MaxUses,
                        link.UseCount,
                        link.CreatedAtUtc,
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            transaction.Commit();
            link.Id = id;
            return link;
        }

        public async Task<SessionGuestLink?> GetActiveGuestLinkBySessionIdAsync(
            int sessionId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<SessionGuestLink>(
                new CommandDefinition(
                    """
                    SELECT Id, SessionId, TokenHash, TokenPlaintext, ExpiresAtUtc, MaxUses, UseCount, CreatedAtUtc
                    FROM SessionGuestLinks
                    WHERE SessionId = @SessionId
                      AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > @Now)
                    ORDER BY CreatedAtUtc DESC
                    LIMIT 1;
                    """,
                    new { SessionId = sessionId, Now = DateTime.UtcNow },
                    cancellationToken: cancellationToken));
        }

        public async Task<SessionGuestLink?> GetActiveGuestLinkByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<SessionGuestLink>(
                new CommandDefinition(
                    """
                    SELECT Id, SessionId, TokenHash, TokenPlaintext, ExpiresAtUtc, MaxUses, UseCount, CreatedAtUtc
                    FROM SessionGuestLinks
                    WHERE TokenHash = @TokenHash
                      AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > @Now);
                    """,
                    new { TokenHash = tokenHash, Now = DateTime.UtcNow },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> TryIncrementGuestLinkUseAsync(int guestLinkId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE SessionGuestLinks
                    SET UseCount = UseCount + 1
                    WHERE Id = @Id
                      AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > @Now)
                      AND (MaxUses IS NULL OR UseCount < MaxUses);
                    """,
                    new { Id = guestLinkId, Now = DateTime.UtcNow },
                    cancellationToken: cancellationToken));

            return rows > 0;
        }

        public async Task<int> EndAbandonedSessionsAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE LiveSessions
                    SET Status = @EndedStatus,
                        EndedAtUtc = @EndedAtUtc
                    WHERE Status IN (@PendingStatus, @ActiveStatus)
                      AND COALESCE(StartedAtUtc, CreatedAtUtc) < @OlderThanUtc;
                    """,
                    new
                    {
                        EndedStatus = LiveSessionStatuses.Ended,
                        PendingStatus = LiveSessionStatuses.Pending,
                        ActiveStatus = LiveSessionStatuses.Active,
                        EndedAtUtc = DateTime.UtcNow,
                        OlderThanUtc = olderThanUtc,
                    },
                    cancellationToken: cancellationToken));
        }

        private static async Task InsertSharesAsync(
            System.Data.IDbConnection connection,
            System.Data.IDbTransaction transaction,
            int sessionId,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<int> userIds,
            CancellationToken cancellationToken)
        {
            foreach (var groupId in groupIds.Distinct())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO SessionGroupShares (SessionId, GroupId, CreatedAtUtc)
                        VALUES (@SessionId, @GroupId, @CreatedAtUtc);
                        """,
                        new { SessionId = sessionId, GroupId = groupId, CreatedAtUtc = DateTime.UtcNow },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            foreach (var userId in userIds.Distinct())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO SessionUserShares (SessionId, UserId, CreatedAtUtc)
                        VALUES (@SessionId, @UserId, @CreatedAtUtc);
                        """,
                        new { SessionId = sessionId, UserId = userId, CreatedAtUtc = DateTime.UtcNow },
                        transaction,
                        cancellationToken: cancellationToken));
            }
        }

        private static async Task PopulateSharesAsync(
            System.Data.IDbConnection connection,
            LiveSession session,
            CancellationToken cancellationToken)
        {
            var groupIds = (await connection.QueryAsync<int>(
                new CommandDefinition(
                    "SELECT GroupId FROM SessionGroupShares WHERE SessionId = @SessionId ORDER BY GroupId;",
                    new { SessionId = session.Id },
                    cancellationToken: cancellationToken))).ToList();

            var userIds = (await connection.QueryAsync<int>(
                new CommandDefinition(
                    "SELECT UserId FROM SessionUserShares WHERE SessionId = @SessionId ORDER BY UserId;",
                    new { SessionId = session.Id },
                    cancellationToken: cancellationToken))).ToList();

            session.GroupIds = groupIds;
            session.UserIds = userIds;
        }
    }
}
