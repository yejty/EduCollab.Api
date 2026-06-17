using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class AssetFolderRepository : IAssetFolderRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AssetFolderRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var folderId = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    """
                    INSERT INTO AssetFolders (
                        WorkspaceId,
                        ParentFolderId,
                        Name,
                        Path,
                        CreatedByUserId,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    SELECT
                        @WorkspaceId,
                        @ParentFolderId,
                        @Name,
                        @Path,
                        @CreatedByUserId,
                        @CreatedAtUtc,
                        @UpdatedAtUtc
                    WHERE @ParentFolderId IS NULL
                       OR EXISTS (
                            SELECT 1
                            FROM AssetFolders parent
                            WHERE parent.Id = @ParentFolderId
                              AND parent.WorkspaceId = @WorkspaceId
                        )
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        folder.ParentFolderId,
                        folder.Name,
                        folder.Path,
                        folder.CreatedByUserId,
                        folder.CreatedAtUtc,
                        folder.UpdatedAtUtc,
                    },
                    cancellationToken: cancellationToken));

            return folderId ?? 0;
        }

        public async Task<List<AssetFolder>> GetAssetFoldersAsync(int workspaceId, int? parentFolderId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var folders = await connection.QueryAsync<AssetFolder>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        ParentFolderId,
                        Name,
                        Path,
                        CreatedByUserId,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM AssetFolders
                    WHERE WorkspaceId = @WorkspaceId
                      AND (
                          (@ParentFolderId IS NULL AND ParentFolderId IS NULL)
                          OR ParentFolderId = @ParentFolderId
                      )
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, ParentFolderId = parentFolderId },
                    cancellationToken: cancellationToken));

            return folders.AsList();
        }

        public async Task<AssetFolder?> GetAssetFolderByIdAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetFolder>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        ParentFolderId,
                        Name,
                        Path,
                        CreatedByUserId,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM AssetFolders
                    WHERE Id = @FolderId
                      AND WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { FolderId = folderId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<AssetFolder?> UpdateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetFolder>(
                new CommandDefinition(
                    """
                    UPDATE AssetFolders AS af
                    SET ParentFolderId = @ParentFolderId,
                        Name = @Name,
                        Path = @Path,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE af.Id = @Id
                      AND af.WorkspaceId = @WorkspaceId
                      AND (
                          @ParentFolderId IS NULL
                          OR EXISTS (
                              SELECT 1
                              FROM AssetFolders parent
                              WHERE parent.Id = @ParentFolderId
                                AND parent.WorkspaceId = @WorkspaceId
                                AND parent.Id <> af.Id
                          )
                      )
                    RETURNING
                        af.Id,
                        af.WorkspaceId,
                        af.ParentFolderId,
                        af.Name,
                        af.Path,
                        af.CreatedByUserId,
                        af.CreatedAtUtc,
                        af.UpdatedAtUtc;
                    """,
                    new
                    {
                        folder.Id,
                        WorkspaceId = workspaceId,
                        folder.ParentFolderId,
                        folder.Name,
                        folder.Path,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task UpdateDescendantPathsAsync(int workspaceId, string oldPathPrefix, string newPathPrefix, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE AssetFolders
                    SET Path = @NewPathPrefix || SUBSTRING(Path FROM CHAR_LENGTH(@OldPathPrefix) + 1)
                    WHERE WorkspaceId = @WorkspaceId
                      AND Path LIKE @SearchPattern;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        OldPathPrefix = oldPathPrefix,
                        NewPathPrefix = newPathPrefix,
                        SearchPattern = oldPathPrefix + "/%"
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteAssetFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM AssetFolders
                    WHERE Id = @FolderId
                      AND WorkspaceId = @WorkspaceId
                      AND NOT EXISTS (
                          SELECT 1
                          FROM AssetFolders child
                          WHERE child.ParentFolderId = @FolderId
                      )
                      AND NOT EXISTS (
                          SELECT 1
                          FROM Assets asset
                          WHERE asset.FolderId = @FolderId
                      );
                    """,
                    new { FolderId = folderId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<AssetFolderGroupShare>> GetAssetFolderSharesAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<AssetFolderGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.FolderId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM AssetFolderGroupShares s
                    INNER JOIN AssetFolders f ON f.Id = s.FolderId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE s.FolderId = @FolderId
                      AND f.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.GroupId;
                    """,
                    new { FolderId = folderId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<List<AssetFolderGroupShare>> GetAssetFolderSharesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<AssetFolderGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.FolderId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM AssetFolderGroupShares s
                    INNER JOIN AssetFolders f ON f.Id = s.FolderId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE s.GroupId = @GroupId
                      AND f.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.FolderId;
                    """,
                    new { GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<AssetFolderGroupShare?> CreateAssetFolderShareAsync(int workspaceId, AssetFolderGroupShare share, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetFolderGroupShare>(
                new CommandDefinition(
                    """
                    INSERT INTO AssetFolderGroupShares (
                        FolderId,
                        GroupId,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @FolderId,
                        @GroupId,
                        @CreatedByUserId,
                        @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM AssetFolders f
                        WHERE f.Id = @FolderId
                          AND f.WorkspaceId = @WorkspaceId
                    )
                      AND EXISTS (
                        SELECT 1
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (FolderId, GroupId) DO NOTHING
                    RETURNING FolderId, GroupId, CreatedByUserId, CreatedAtUtc;
                    """,
                    new
                    {
                        share.FolderId,
                        share.GroupId,
                        share.CreatedByUserId,
                        share.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteAssetFolderShareAsync(int workspaceId, int folderId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM AssetFolderGroupShares s
                    USING AssetFolders f, Groups g
                    WHERE s.FolderId = @FolderId
                      AND s.GroupId = @GroupId
                      AND f.Id = s.FolderId
                      AND g.Id = s.GroupId
                      AND f.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId;
                    """,
                    new { FolderId = folderId, GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<AssetFolderGroupShare>> GetWorkspaceAssetFolderSharesAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<AssetFolderGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.FolderId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM AssetFolderGroupShares s
                    INNER JOIN AssetFolders f ON f.Id = s.FolderId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE f.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.FolderId, s.GroupId;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }
    }
}
