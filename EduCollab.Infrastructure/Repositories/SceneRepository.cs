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
                    SELECT
                        @WorkspaceId,
                        @OwnerUserId,
                        @GroupId,
                        @Name,
                        @Description,
                        CAST(@JsonContent AS jsonb),
                        '',
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

        public async Task<List<Scene>> GetScenesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var scenes = await connection.QueryAsync<Scene>(
                new CommandDefinition(
                    $"""
                    SELECT {SceneListColumns}
                    FROM Scenes
                    WHERE WorkspaceId = @WorkspaceId
                      AND GroupId = @GroupId
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
                        scene.GroupId,
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
    }
}
