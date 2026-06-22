using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class SceneRepository : ISceneRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

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
                        Name,
                        Description,
                        JsonContent,
                        ETag,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    VALUES (
                        @WorkspaceId,
                        @OwnerUserId,
                        @Name,
                        @Description,
                        CAST(@JsonContent AS jsonb),
                        @ETag,
                        @CurrentVersionNumber,
                        @CreatedAtUtc,
                        @UpdatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        scene.OwnerUserId,
                        scene.Name,
                        scene.Description,
                        JsonContent = "{}",
                        scene.ETag,
                        scene.CurrentVersionNumber,
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
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        OwnerUserId,
                        Name,
                        Description,
                        '' AS JsonContent,
                        ETag,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc
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
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        OwnerUserId,
                        Name,
                        Description,
                        '' AS JsonContent,
                        ETag,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM Scenes
                    WHERE WorkspaceId = @WorkspaceId
                      AND OwnerUserId = @OwnerUserId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, OwnerUserId = ownerUserId },
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
                        Name,
                        Description,
                        JsonContent::text AS JsonContent,
                        ETag,
                        CurrentVersionNumber,
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
                        ETag = @ETag,
                        CurrentVersionNumber = @CurrentVersionNumber,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @Id
                      AND WorkspaceId = @WorkspaceId
                    RETURNING
                        Id,
                        WorkspaceId,
                        OwnerUserId,
                        Name,
                        Description,
                        '' AS JsonContent,
                        ETag,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc;
                    """,
                    new
                    {
                        scene.Id,
                        WorkspaceId = workspaceId,
                        scene.Name,
                        scene.Description,
                        scene.ETag,
                        scene.CurrentVersionNumber,
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

        public async Task<SceneVersion?> CreateSceneVersionAsync(int workspaceId, SceneVersion version, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<SceneVersion>(
                new CommandDefinition(
                    """
                    INSERT INTO SceneVersions (
                        SceneId,
                        VersionNumber,
                        Name,
                        Description,
                        ETag,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @SceneId,
                        @VersionNumber,
                        @Name,
                        @Description,
                        @ETag,
                        @CreatedByUserId,
                        @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Scenes s
                        WHERE s.Id = @SceneId
                          AND s.WorkspaceId = @WorkspaceId
                    )
                    RETURNING
                        SceneId,
                        VersionNumber,
                        Name,
                        Description,
                        ETag,
                        CreatedByUserId,
                        CreatedAtUtc;
                    """,
                    new
                    {
                        version.SceneId,
                        version.VersionNumber,
                        version.Name,
                        version.Description,
                        version.ETag,
                        version.CreatedByUserId,
                        version.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<List<SceneVersion>> GetSceneVersionsAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var versions = await connection.QueryAsync<SceneVersion>(
                new CommandDefinition(
                    """
                    SELECT
                        v.SceneId,
                        v.VersionNumber,
                        v.Name,
                        v.Description,
                        v.ETag,
                        v.CreatedByUserId,
                        v.CreatedAtUtc
                    FROM SceneVersions v
                    INNER JOIN Scenes s ON s.Id = v.SceneId
                    WHERE v.SceneId = @SceneId
                      AND s.WorkspaceId = @WorkspaceId
                    ORDER BY v.VersionNumber DESC;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return versions.AsList();
        }

        public async Task<SceneVersion?> GetSceneVersionAsync(int workspaceId, int sceneId, int versionNumber, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<SceneVersion>(
                new CommandDefinition(
                    """
                    SELECT
                        v.SceneId,
                        v.VersionNumber,
                        v.Name,
                        v.Description,
                        v.ETag,
                        v.CreatedByUserId,
                        v.CreatedAtUtc
                    FROM SceneVersions v
                    INNER JOIN Scenes s ON s.Id = v.SceneId
                    WHERE v.SceneId = @SceneId
                      AND v.VersionNumber = @VersionNumber
                      AND s.WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { SceneId = sceneId, VersionNumber = versionNumber, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdateSceneCurrentVersionAsync(int workspaceId, int sceneId, int currentVersionNumber, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Scenes
                    SET CurrentVersionNumber = @CurrentVersionNumber,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @SceneId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new
                    {
                        SceneId = sceneId,
                        WorkspaceId = workspaceId,
                        CurrentVersionNumber = currentVersionNumber,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));

            return updated > 0;
        }

        public async Task<List<SceneGroupShare>> GetSceneSharesAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<SceneGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.SceneId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM SceneGroupShares s
                    INNER JOIN Scenes sc ON sc.Id = s.SceneId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE s.SceneId = @SceneId
                      AND sc.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.GroupId;
                    """,
                    new { SceneId = sceneId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<List<SceneGroupShare>> GetWorkspaceSceneSharesAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<SceneGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.SceneId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM SceneGroupShares s
                    INNER JOIN Scenes sc ON sc.Id = s.SceneId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE sc.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.SceneId, s.GroupId;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<List<SceneGroupShare>> GetSceneSharesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<SceneGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.SceneId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM SceneGroupShares s
                    INNER JOIN Scenes sc ON sc.Id = s.SceneId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE s.GroupId = @GroupId
                      AND sc.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.SceneId;
                    """,
                    new { GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<SceneGroupShare?> CreateSceneShareAsync(int workspaceId, SceneGroupShare share, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<SceneGroupShare>(
                new CommandDefinition(
                    """
                    INSERT INTO SceneGroupShares (
                        SceneId,
                        GroupId,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @SceneId,
                        @GroupId,
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
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (SceneId, GroupId) DO NOTHING
                    RETURNING SceneId, GroupId, CreatedByUserId, CreatedAtUtc;
                    """,
                    new
                    {
                        share.SceneId,
                        share.GroupId,
                        share.CreatedByUserId,
                        share.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteSceneShareAsync(int workspaceId, int sceneId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM SceneGroupShares s
                    USING Scenes sc, Groups g
                    WHERE s.SceneId = @SceneId
                      AND s.GroupId = @GroupId
                      AND sc.Id = s.SceneId
                      AND g.Id = s.GroupId
                      AND sc.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId;
                    """,
                    new { SceneId = sceneId, GroupId = groupId, WorkspaceId = workspaceId },
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
