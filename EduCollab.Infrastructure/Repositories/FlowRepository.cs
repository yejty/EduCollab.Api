using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class FlowRepository : IFlowRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string FlowSelectColumns =
            """
            Id,
            WorkspaceId,
            OwnerUserId,
            GroupId,
            Name,
            Description,
            CreatedAtUtc,
            UpdatedAtUtc
            """;

        public FlowRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateFlowAsync(int workspaceId, Flow flow, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var flowId = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    """
                    INSERT INTO Flows (
                        WorkspaceId,
                        OwnerUserId,
                        GroupId,
                        Name,
                        Description,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    VALUES (
                        @WorkspaceId,
                        @OwnerUserId,
                        @GroupId,
                        @Name,
                        @Description,
                        @CreatedAtUtc,
                        @UpdatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        flow.OwnerUserId,
                        flow.GroupId,
                        flow.Name,
                        flow.Description,
                        flow.CreatedAtUtc,
                        flow.UpdatedAtUtc
                    },
                    cancellationToken: cancellationToken));

            return flowId ?? 0;
        }

        public async Task<List<Flow>> GetAllFlowsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var flows = await connection.QueryAsync<Flow>(
                new CommandDefinition(
                    $"""
                    SELECT {FlowSelectColumns}
                    FROM Flows
                    WHERE WorkspaceId = @WorkspaceId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return flows.AsList();
        }

        public async Task<List<Flow>> GetFlowsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var flows = await connection.QueryAsync<Flow>(
                new CommandDefinition(
                    $"""
                    SELECT {FlowSelectColumns}
                    FROM Flows
                    WHERE WorkspaceId = @WorkspaceId
                      AND OwnerUserId = @OwnerUserId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, OwnerUserId = ownerUserId },
                    cancellationToken: cancellationToken));

            return flows.AsList();
        }

        public async Task<List<Flow>> GetFlowsByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var flows = await connection.QueryAsync<Flow>(
                new CommandDefinition(
                    $"""
                    SELECT DISTINCT {FlowSelectColumns}
                    FROM Flows f
                    INNER JOIN FlowGroupShares fgs ON fgs.FlowId = f.Id
                    WHERE f.WorkspaceId = @WorkspaceId
                      AND fgs.GroupId = @GroupId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, GroupId = groupId },
                    cancellationToken: cancellationToken));

            return flows.AsList();
        }

        public async Task<Flow?> GetFlowByIdAsync(int workspaceId, int flowId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Flow>(
                new CommandDefinition(
                    $"""
                    SELECT {FlowSelectColumns}
                    FROM Flows
                    WHERE Id = @FlowId
                      AND WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { FlowId = flowId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<Flow?> UpdateFlowAsync(int workspaceId, Flow flow, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Flow>(
                new CommandDefinition(
                    $"""
                    UPDATE Flows
                    SET Name = @Name,
                        Description = @Description,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @Id
                      AND WorkspaceId = @WorkspaceId
                    RETURNING {FlowSelectColumns};
                    """,
                    new
                    {
                        flow.Id,
                        WorkspaceId = workspaceId,
                        flow.Name,
                        flow.Description,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteFlowAsync(int workspaceId, int flowId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM Flows
                    WHERE Id = @FlowId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new { FlowId = flowId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<FlowSceneLink>> GetFlowSceneLinksAsync(int workspaceId, int flowId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var links = await connection.QueryAsync<FlowSceneLink>(
                new CommandDefinition(
                    """
                    SELECT
                        fs.FlowId,
                        fs.SceneId,
                        fs.CreatedByUserId,
                        fs.CreatedAtUtc
                    FROM FlowScenes fs
                    INNER JOIN Flows f ON f.Id = fs.FlowId
                    WHERE fs.FlowId = @FlowId
                      AND f.WorkspaceId = @WorkspaceId
                    ORDER BY fs.CreatedAtUtc, fs.SceneId;
                    """,
                    new { FlowId = flowId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return links.AsList();
        }

        public async Task<FlowSceneLink?> CreateFlowSceneLinkAsync(int workspaceId, FlowSceneLink link, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<FlowSceneLink>(
                new CommandDefinition(
                    """
                    INSERT INTO FlowScenes (
                        FlowId,
                        SceneId,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @FlowId,
                        @SceneId,
                        @CreatedByUserId,
                        @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Flows f
                        WHERE f.Id = @FlowId
                          AND f.WorkspaceId = @WorkspaceId
                    )
                      AND EXISTS (
                        SELECT 1
                        FROM Scenes s
                        WHERE s.Id = @SceneId
                          AND s.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (FlowId, SceneId) DO NOTHING
                    RETURNING FlowId, SceneId, CreatedByUserId, CreatedAtUtc;
                    """,
                    new
                    {
                        link.FlowId,
                        link.SceneId,
                        link.CreatedByUserId,
                        link.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteFlowSceneLinkAsync(int workspaceId, int flowId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM FlowScenes fs
                    USING Flows f, Scenes s
                    WHERE fs.FlowId = @FlowId
                      AND fs.SceneId = @SceneId
                      AND f.Id = fs.FlowId
                      AND s.Id = fs.SceneId
                      AND f.WorkspaceId = @WorkspaceId
                      AND s.WorkspaceId = @WorkspaceId;
                    """,
                    new { FlowId = flowId, SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<int>> GetFlowGroupIdsAsync(int workspaceId, int flowId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var groupIds = await connection.QueryAsync<int>(
                new CommandDefinition(
                    """
                    SELECT fgs.GroupId
                    FROM FlowGroupShares fgs
                    INNER JOIN Flows f ON f.Id = fgs.FlowId
                    WHERE fgs.FlowId = @FlowId
                      AND f.WorkspaceId = @WorkspaceId
                    ORDER BY fgs.CreatedAtUtc, fgs.GroupId;
                    """,
                    new { FlowId = flowId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return groupIds.AsList();
        }

        public async Task<Dictionary<int, List<int>>> GetFlowGroupIdsByFlowIdsAsync(
            int workspaceId,
            IReadOnlyCollection<int> flowIds,
            CancellationToken cancellationToken)
        {
            if (flowIds.Count == 0)
                return new Dictionary<int, List<int>>();

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var rows = await connection.QueryAsync<(int FlowId, int GroupId)>(
                new CommandDefinition(
                    """
                    SELECT fgs.FlowId, fgs.GroupId
                    FROM FlowGroupShares fgs
                    INNER JOIN Flows f ON f.Id = fgs.FlowId
                    WHERE f.WorkspaceId = @WorkspaceId
                      AND fgs.FlowId = ANY(@FlowIds)
                    ORDER BY fgs.FlowId, fgs.CreatedAtUtc, fgs.GroupId;
                    """,
                    new { WorkspaceId = workspaceId, FlowIds = flowIds.ToArray() },
                    cancellationToken: cancellationToken));

            return rows
                .GroupBy(row => row.FlowId)
                .ToDictionary(group => group.Key, group => group.Select(row => row.GroupId).ToList());
        }

        public async Task ReplaceFlowGroupSharesAsync(
            int workspaceId,
            int flowId,
            IReadOnlyList<int> groupIds,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM FlowGroupShares fgs
                    USING Flows f
                    WHERE fgs.FlowId = @FlowId
                      AND f.Id = fgs.FlowId
                      AND f.WorkspaceId = @WorkspaceId;
                    """,
                    new { FlowId = flowId, WorkspaceId = workspaceId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var createdAtUtc = DateTime.UtcNow;
            foreach (var groupId in groupIds.Distinct())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO FlowGroupShares (FlowId, GroupId, CreatedAtUtc)
                        SELECT @FlowId, @GroupId, @CreatedAtUtc
                        WHERE EXISTS (
                            SELECT 1
                            FROM Flows f
                            WHERE f.Id = @FlowId
                              AND f.WorkspaceId = @WorkspaceId
                        )
                          AND EXISTS (
                            SELECT 1
                            FROM Groups g
                            WHERE g.Id = @GroupId
                              AND g.WorkspaceId = @WorkspaceId
                        );
                        """,
                        new
                        {
                            FlowId = flowId,
                            GroupId = groupId,
                            CreatedAtUtc = createdAtUtc,
                            WorkspaceId = workspaceId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }

            transaction.Commit();
        }

        public async Task<bool> AddFlowGroupShareAsync(
            int workspaceId,
            int flowId,
            int groupId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO FlowGroupShares (FlowId, GroupId, CreatedAtUtc)
                    SELECT @FlowId, @GroupId, @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Flows f
                        WHERE f.Id = @FlowId
                          AND f.WorkspaceId = @WorkspaceId
                    )
                      AND EXISTS (
                        SELECT 1
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (FlowId, GroupId) DO NOTHING;
                    """,
                    new
                    {
                        FlowId = flowId,
                        GroupId = groupId,
                        CreatedAtUtc = DateTime.UtcNow,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));

            return inserted > 0;
        }

        public async Task<bool> RemoveFlowGroupShareAsync(
            int workspaceId,
            int flowId,
            int groupId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM FlowGroupShares fgs
                    USING Flows f
                    WHERE fgs.FlowId = @FlowId
                      AND fgs.GroupId = @GroupId
                      AND f.Id = fgs.FlowId
                      AND f.WorkspaceId = @WorkspaceId;
                    """,
                    new { FlowId = flowId, GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task SyncFlowPrimaryGroupIdAsync(int workspaceId, int flowId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Flows f
                    SET GroupId = (
                        SELECT fgs.GroupId
                        FROM FlowGroupShares fgs
                        WHERE fgs.FlowId = f.Id
                        ORDER BY fgs.CreatedAtUtc, fgs.GroupId
                        LIMIT 1
                    ),
                    UpdatedAtUtc = @UpdatedAtUtc
                    WHERE f.Id = @FlowId
                      AND f.WorkspaceId = @WorkspaceId;
                    """,
                    new
                    {
                        FlowId = flowId,
                        WorkspaceId = workspaceId,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
