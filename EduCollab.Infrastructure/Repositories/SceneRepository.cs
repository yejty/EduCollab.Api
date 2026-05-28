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
    }
}
