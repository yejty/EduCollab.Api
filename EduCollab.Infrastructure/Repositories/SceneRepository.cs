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
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    VALUES (
                        @WorkspaceId,
                        @OwnerUserId,
                        @Name,
                        @Description,
                        CAST(@JsonContent AS jsonb),
                        @ETag,
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
                        scene.JsonContent,
                        scene.ETag,
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
                        JsonContent::text AS JsonContent,
                        ETag,
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
                        JsonContent::text AS JsonContent,
                        ETag,
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
                        JsonContent = CAST(@JsonContent AS jsonb),
                        ETag = @ETag,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @Id
                      AND WorkspaceId = @WorkspaceId
                    RETURNING
                        Id,
                        WorkspaceId,
                        OwnerUserId,
                        Name,
                        Description,
                        JsonContent::text AS JsonContent,
                        ETag,
                        CreatedAtUtc,
                        UpdatedAtUtc;
                    """,
                    new
                    {
                        scene.Id,
                        WorkspaceId = workspaceId,
                        scene.Name,
                        scene.Description,
                        scene.JsonContent,
                        scene.ETag,
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
    }
}
