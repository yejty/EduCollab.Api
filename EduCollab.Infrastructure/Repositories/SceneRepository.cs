using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class SceneRepository : ISceneRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string SceneListColumns =
            """
            Id,
            WorkspaceId,
            OwnerUserId,
            GroupId,
            Name,
            Description,
            '' AS JsonContent,
            CreatedAtUtc,
            UpdatedAtUtc
            """;

        public SceneRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var sceneId = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    """
                    INSERT INTO Scenes (
                        WorkspaceId,
                        OwnerUserId,
                        GroupId,
                        Name,
                        Description,
                        JsonContent,
                        ETag,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    VALUES (
                        @WorkspaceId,
                        @OwnerUserId,
                        @GroupId,
                        @Name,
                        @Description,
                        CAST(@JsonContent AS jsonb),
                        '',
                        @CreatedAtUtc,
                        @UpdatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        scene.OwnerUserId,
                        scene.GroupId,
                        scene.Name,
                        scene.Description,
                        JsonContent = "{}",
                        scene.CreatedAtUtc,
                        scene.UpdatedAtUtc
                    },
                    cancellationToken: cancellationToken));

            return sceneId ?? 0;
        }

        public async Task<List<Scene>> GetAllScenesAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var scenes = await connection.QueryAsync<Scene>(
                new CommandDefinition(
                    $"""
                    SELECT {SceneListColumns}
                    FROM Scenes
                    WHERE WorkspaceId = @WorkspaceId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return scenes.AsList();
        }

        public async Task<List<Scene>> GetScenesByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var scenes = await connection.QueryAsync<Scene>(
                new CommandDefinition(
                    $"""
                    SELECT {SceneListColumns}
                    FROM Scenes
                    WHERE WorkspaceId = @WorkspaceId
                      AND OwnerUserId = @OwnerUserId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, OwnerUserId = ownerUserId },
                    cancellationToken: cancellationToken));

            return scenes.AsList();
        }

        public async Task<List<Scene>> GetScenesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var scenes = await connection.QueryAsync<Scene>(
                new CommandDefinition(
                    $"""
                    SELECT DISTINCT {SceneListColumns}
                    FROM Scenes s
                    INNER JOIN SceneGroupShares sgs ON sgs.SceneId = s.Id
                    WHERE s.WorkspaceId = @WorkspaceId
                      AND sgs.GroupId = @GroupId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, GroupId = groupId },
                    cancellationToken: cancellationToken));

            return scenes.AsList();
        }

        public async Task<Scene?> GetSceneByIdAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Scene>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        OwnerUserId,
                        GroupId,
                        Name,
                        Description,
                        JsonContent::text AS JsonContent,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM Scenes
                    WHERE Id = @SceneId
                      AND WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<Scene?> UpdateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Scene>(
                new CommandDefinition(
                    """
                    UPDATE Scenes
                    SET Name = @Name,
                        Description = @Description,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @Id
                      AND WorkspaceId = @WorkspaceId
                    RETURNING
                        Id,
                        WorkspaceId,
                        OwnerUserId,
                        GroupId,
                        Name,
                        Description,
                        '' AS JsonContent,
                        CreatedAtUtc,
                        UpdatedAtUtc;
                    """,
                    new
                    {
                        scene.Id,
                        WorkspaceId = workspaceId,
                        scene.Name,
                        scene.Description,
                        UpdatedAtUtc = scene.UpdatedAtUtc
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteSceneAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM Scenes
                    WHERE Id = @SceneId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<SceneAssetLink>> GetSceneAssetLinksAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var links = await connection.QueryAsync<SceneAssetLink>(
                new CommandDefinition(
                    """
                    SELECT
                        sa.SceneId,
                        sa.AssetId,
                        sa.CreatedByUserId,
                        sa.CreatedAtUtc
                    FROM SceneAssets sa
                    INNER JOIN Scenes sc ON sc.Id = sa.SceneId
                    WHERE sa.SceneId = @SceneId
                      AND sc.WorkspaceId = @WorkspaceId
                    ORDER BY sa.CreatedAtUtc, sa.AssetId;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return links.AsList();
        }

        public async Task<SceneAssetLink?> CreateSceneAssetLinkAsync(int workspaceId, SceneAssetLink link, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<SceneAssetLink>(
                new CommandDefinition(
                    """
                    INSERT INTO SceneAssets (
                        SceneId,
                        AssetId,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @SceneId,
                        @AssetId,
                        @CreatedByUserId,
                        @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Scenes sc
                        WHERE sc.Id = @SceneId
                          AND sc.WorkspaceId = @WorkspaceId
                    )
                      AND EXISTS (
                        SELECT 1
                        FROM Assets a
                        WHERE a.Id = @AssetId
                          AND a.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (SceneId, AssetId) DO NOTHING
                    RETURNING SceneId, AssetId, CreatedByUserId, CreatedAtUtc;
                    """,
                    new
                    {
                        link.SceneId,
                        link.AssetId,
                        link.CreatedByUserId,
                        link.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteSceneAssetLinkAsync(int workspaceId, int sceneId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM SceneAssets sa
                    USING Scenes sc, Assets a
                    WHERE sa.SceneId = @SceneId
                      AND sa.AssetId = @AssetId
                      AND sc.Id = sa.SceneId
                      AND a.Id = sa.AssetId
                      AND sc.WorkspaceId = @WorkspaceId
                      AND a.WorkspaceId = @WorkspaceId;
                    """,
                    new { SceneId = sceneId, AssetId = assetId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<int>> GetSceneGroupIdsAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var groupIds = await connection.QueryAsync<int>(
                new CommandDefinition(
                    """
                    SELECT sgs.GroupId
                    FROM SceneGroupShares sgs
                    INNER JOIN Scenes s ON s.Id = sgs.SceneId
                    WHERE sgs.SceneId = @SceneId
                      AND s.WorkspaceId = @WorkspaceId
                    ORDER BY sgs.CreatedAtUtc, sgs.GroupId;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return groupIds.AsList();
        }

        public async Task<Dictionary<int, List<int>>> GetSceneGroupIdsBySceneIdsAsync(
            int workspaceId,
            IReadOnlyCollection<int> sceneIds,
            CancellationToken cancellationToken)
        {
            if (sceneIds.Count == 0)
                return new Dictionary<int, List<int>>();

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var rows = await connection.QueryAsync<(int SceneId, int GroupId)>(
                new CommandDefinition(
                    """
                    SELECT sgs.SceneId, sgs.GroupId
                    FROM SceneGroupShares sgs
                    INNER JOIN Scenes s ON s.Id = sgs.SceneId
                    WHERE s.WorkspaceId = @WorkspaceId
                      AND sgs.SceneId = ANY(@SceneIds)
                    ORDER BY sgs.SceneId, sgs.CreatedAtUtc, sgs.GroupId;
                    """,
                    new { WorkspaceId = workspaceId, SceneIds = sceneIds.ToArray() },
                    cancellationToken: cancellationToken));

            return rows
                .GroupBy(row => row.SceneId)
                .ToDictionary(group => group.Key, group => group.Select(row => row.GroupId).ToList());
        }

        public async Task ReplaceSceneGroupSharesAsync(
            int workspaceId,
            int sceneId,
            IReadOnlyList<int> groupIds,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM SceneGroupShares sgs
                    USING Scenes s
                    WHERE sgs.SceneId = @SceneId
                      AND s.Id = sgs.SceneId
                      AND s.WorkspaceId = @WorkspaceId;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var createdAtUtc = DateTime.UtcNow;
            foreach (var groupId in groupIds.Distinct())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO SceneGroupShares (SceneId, GroupId, CreatedAtUtc)
                        SELECT @SceneId, @GroupId, @CreatedAtUtc
                        WHERE EXISTS (
                            SELECT 1
                            FROM Scenes s
                            WHERE s.Id = @SceneId
                              AND s.WorkspaceId = @WorkspaceId
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
                            SceneId = sceneId,
                            GroupId = groupId,
                            CreatedAtUtc = createdAtUtc,
                            WorkspaceId = workspaceId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }

            transaction.Commit();
        }

        public async Task<bool> AddSceneGroupShareAsync(
            int workspaceId,
            int sceneId,
            int groupId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO SceneGroupShares (SceneId, GroupId, CreatedAtUtc)
                    SELECT @SceneId, @GroupId, @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Scenes s
                        WHERE s.Id = @SceneId
                          AND s.WorkspaceId = @WorkspaceId
                    )
                      AND EXISTS (
                        SELECT 1
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (SceneId, GroupId) DO NOTHING;
                    """,
                    new
                    {
                        SceneId = sceneId,
                        GroupId = groupId,
                        CreatedAtUtc = DateTime.UtcNow,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));

            return inserted > 0;
        }

        public async Task<bool> RemoveSceneGroupShareAsync(
            int workspaceId,
            int sceneId,
            int groupId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM SceneGroupShares sgs
                    USING Scenes s
                    WHERE sgs.SceneId = @SceneId
                      AND sgs.GroupId = @GroupId
                      AND s.Id = sgs.SceneId
                      AND s.WorkspaceId = @WorkspaceId;
                    """,
                    new { SceneId = sceneId, GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task SyncScenePrimaryGroupIdAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Scenes s
                    SET GroupId = (
                        SELECT sgs.GroupId
                        FROM SceneGroupShares sgs
                        WHERE sgs.SceneId = s.Id
                        ORDER BY sgs.CreatedAtUtc, sgs.GroupId
                        LIMIT 1
                    ),
                    UpdatedAtUtc = @UpdatedAtUtc
                    WHERE s.Id = @SceneId
                      AND s.WorkspaceId = @WorkspaceId;
                    """,
                    new
                    {
                        SceneId = sceneId,
                        WorkspaceId = workspaceId,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
