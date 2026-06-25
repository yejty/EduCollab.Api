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
                    SELECT
                        @WorkspaceId,
                        @OwnerUserId,
                        @GroupId,
                        @Name,
                        @Description,
                        @CreatedAtUtc,
                        @UpdatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
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

        public async Task<List<Flow>> GetFlowsByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var flows = await connection.QueryAsync<Flow>(
                new CommandDefinition(
                    $"""
                    SELECT {FlowSelectColumns}
                    FROM Flows
                    WHERE WorkspaceId = @WorkspaceId
                      AND GroupId = @GroupId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, GroupId = groupId },
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
                        GroupId = @GroupId,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @Id
                      AND WorkspaceId = @WorkspaceId
                      AND EXISTS (
                          SELECT 1
                          FROM Groups g
                          WHERE g.Id = @GroupId
                            AND g.WorkspaceId = @WorkspaceId
                      )
                    RETURNING {FlowSelectColumns};
                    """,
                    new
                    {
                        flow.Id,
                        WorkspaceId = workspaceId,
                        flow.GroupId,
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

        public async Task<List<FlowScene>> GetFlowScenesAsync(int workspaceId, int flowId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var flowScenes = await connection.QueryAsync<FlowScene>(
                new CommandDefinition(
                    """
                    SELECT
                        fs.FlowId,
                        fs.SceneId,
                        fs.SortOrder
                    FROM FlowScenes fs
                    INNER JOIN Flows f ON f.Id = fs.FlowId
                    WHERE fs.FlowId = @FlowId
                      AND f.WorkspaceId = @WorkspaceId
                    ORDER BY fs.SortOrder, fs.SceneId;
                    """,
                    new { FlowId = flowId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return flowScenes.AsList();
        }

        public async Task<FlowScene?> AddFlowSceneAsync(int workspaceId, FlowScene flowScene, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<FlowScene>(
                new CommandDefinition(
                    """
                    INSERT INTO FlowScenes (FlowId, SceneId, SortOrder)
                    SELECT @FlowId, @SceneId, @SortOrder
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
                    RETURNING FlowId, SceneId, SortOrder;
                    """,
                    new
                    {
                        flowScene.FlowId,
                        flowScene.SceneId,
                        flowScene.SortOrder,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> RemoveFlowSceneAsync(int workspaceId, int flowId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM FlowScenes fs
                    USING Flows f
                    WHERE fs.FlowId = @FlowId
                      AND fs.SceneId = @SceneId
                      AND f.Id = fs.FlowId
                      AND f.WorkspaceId = @WorkspaceId;
                    """,
                    new { FlowId = flowId, SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }
    }
}
