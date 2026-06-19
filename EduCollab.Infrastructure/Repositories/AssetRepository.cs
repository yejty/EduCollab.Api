using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class AssetRepository : IAssetRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AssetRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assetId = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    """
                    INSERT INTO Assets (
                        WorkspaceId,
                        FolderId,
                        OwnerUserId,
                        Name,
                        Description,
                        AssetType,
                        StorageUrl,
                        Version,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    SELECT
                        @WorkspaceId,
                        @FolderId,
                        @OwnerUserId,
                        @Name,
                        @Description,
                        @AssetType,
                        @StorageUrl,
                        @Version,
                        @CurrentVersionNumber,
                        @CreatedAtUtc,
                        @UpdatedAtUtc
                    WHERE @FolderId IS NULL
                       OR EXISTS (
                            SELECT 1
                            FROM AssetFolders f
                            WHERE f.Id = @FolderId
                              AND f.WorkspaceId = @WorkspaceId
                        )
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        asset.FolderId,
                        asset.OwnerUserId,
                        asset.Name,
                        asset.Description,
                        asset.AssetType,
                        asset.StorageUrl,
                        asset.Version,
                        asset.CurrentVersionNumber,
                        asset.CreatedAtUtc,
                        asset.UpdatedAtUtc,
                    },
                    cancellationToken: cancellationToken));

            return assetId ?? 0;
        }

        public async Task<List<Asset>> GetAllAssetsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assets = await connection.QueryAsync<Asset>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        FolderId,
                        OwnerUserId,
                        Name,
                        Description,
                        AssetType,
                        StorageUrl,
                        Version,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM Assets
                    WHERE WorkspaceId = @WorkspaceId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return assets.AsList();
        }

        public async Task<List<Asset>> GetAssetsByFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assets = await connection.QueryAsync<Asset>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        FolderId,
                        OwnerUserId,
                        Name,
                        Description,
                        AssetType,
                        StorageUrl,
                        Version,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM Assets
                    WHERE WorkspaceId = @WorkspaceId
                      AND FolderId = @FolderId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, FolderId = folderId },
                    cancellationToken: cancellationToken));

            return assets.AsList();
        }

        public async Task<List<Asset>> GetAssetsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assets = await connection.QueryAsync<Asset>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        FolderId,
                        OwnerUserId,
                        Name,
                        Description,
                        AssetType,
                        StorageUrl,
                        Version,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM Assets
                    WHERE WorkspaceId = @WorkspaceId
                      AND OwnerUserId = @OwnerUserId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, OwnerUserId = ownerUserId },
                    cancellationToken: cancellationToken));

            return assets.AsList();
        }

        public async Task<Asset?> GetAssetByIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Asset>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        WorkspaceId,
                        FolderId,
                        OwnerUserId,
                        Name,
                        Description,
                        AssetType,
                        StorageUrl,
                        Version,
                        CurrentVersionNumber,
                        CreatedAtUtc,
                        UpdatedAtUtc
                    FROM Assets
                    WHERE Id = @AssetId
                      AND WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { AssetId = assetId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<Asset?> UpdateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Asset>(
                new CommandDefinition(
                    """
                    UPDATE Assets AS a
                    SET FolderId = @FolderId,
                        Name = @Name,
                        Description = @Description,
                        AssetType = @AssetType,
                        Version = @Version,
                        CurrentVersionNumber = @CurrentVersionNumber,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE a.Id = @Id
                      AND a.WorkspaceId = @WorkspaceId
                      AND (
                          @FolderId IS NULL
                          OR EXISTS (
                              SELECT 1
                              FROM AssetFolders f
                              WHERE f.Id = @FolderId
                                AND f.WorkspaceId = @WorkspaceId
                          )
                      )
                    RETURNING
                        a.Id,
                        a.WorkspaceId,
                        a.FolderId,
                        a.OwnerUserId,
                        a.Name,
                        a.Description,
                        a.AssetType,
                        a.StorageUrl,
                        a.Version,
                        a.CurrentVersionNumber,
                        a.CreatedAtUtc,
                        a.UpdatedAtUtc;
                    """,
                    new
                    {
                        asset.Id,
                        WorkspaceId = workspaceId,
                        asset.FolderId,
                        asset.Name,
                        asset.Description,
                        asset.AssetType,
                        asset.Version,
                        asset.CurrentVersionNumber,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task UpdateAssetStorageUrlAsync(int workspaceId, int assetId, string storageUrl, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Assets
                    SET StorageUrl = @StorageUrl,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @AssetId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new
                    {
                        AssetId = assetId,
                        WorkspaceId = workspaceId,
                        StorageUrl = storageUrl,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteAssetAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM Assets
                    WHERE Id = @AssetId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new { AssetId = assetId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<Asset?> MoveAssetAsync(int workspaceId, int assetId, int? folderId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Asset>(
                new CommandDefinition(
                    """
                    UPDATE Assets AS a
                    SET FolderId = @FolderId,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE a.Id = @AssetId
                      AND a.WorkspaceId = @WorkspaceId
                      AND (
                          @FolderId IS NULL
                          OR EXISTS (
                              SELECT 1
                              FROM AssetFolders f
                              WHERE f.Id = @FolderId
                                AND f.WorkspaceId = @WorkspaceId
                          )
                      )
                    RETURNING
                        a.Id,
                        a.WorkspaceId,
                        a.FolderId,
                        a.OwnerUserId,
                        a.Name,
                        a.Description,
                        a.AssetType,
                        a.StorageUrl,
                        a.Version,
                        a.CurrentVersionNumber,
                        a.CreatedAtUtc,
                        a.UpdatedAtUtc;
                    """,
                    new
                    {
                        AssetId = assetId,
                        WorkspaceId = workspaceId,
                        FolderId = folderId,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<List<AssetGroupShare>> GetAssetSharesAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<AssetGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.AssetId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM AssetGroupShares s
                    INNER JOIN Assets a ON a.Id = s.AssetId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE s.AssetId = @AssetId
                      AND a.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.GroupId;
                    """,
                    new { AssetId = assetId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<List<AssetGroupShare>> GetAssetSharesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<AssetGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.AssetId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM AssetGroupShares s
                    INNER JOIN Assets a ON a.Id = s.AssetId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE s.GroupId = @GroupId
                      AND a.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.AssetId;
                    """,
                    new { GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<AssetGroupShare?> CreateAssetShareAsync(int workspaceId, AssetGroupShare share, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetGroupShare>(
                new CommandDefinition(
                    """
                    INSERT INTO AssetGroupShares (
                        AssetId,
                        GroupId,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @AssetId,
                        @GroupId,
                        @CreatedByUserId,
                        @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Assets a
                        WHERE a.Id = @AssetId
                          AND a.WorkspaceId = @WorkspaceId
                    )
                      AND EXISTS (
                        SELECT 1
                        FROM Groups g
                        WHERE g.Id = @GroupId
                          AND g.WorkspaceId = @WorkspaceId
                    )
                    ON CONFLICT (AssetId, GroupId) DO NOTHING
                    RETURNING AssetId, GroupId, CreatedByUserId, CreatedAtUtc;
                    """,
                    new
                    {
                        share.AssetId,
                        share.GroupId,
                        share.CreatedByUserId,
                        share.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteAssetShareAsync(int workspaceId, int assetId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM AssetGroupShares s
                    USING Assets a, Groups g
                    WHERE s.AssetId = @AssetId
                      AND s.GroupId = @GroupId
                      AND a.Id = s.AssetId
                      AND g.Id = s.GroupId
                      AND a.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId;
                    """,
                    new { AssetId = assetId, GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task<List<AssetGroupShare>> GetWorkspaceAssetSharesAsync(int workspaceId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var shares = await connection.QueryAsync<AssetGroupShare>(
                new CommandDefinition(
                    """
                    SELECT
                        s.AssetId,
                        s.GroupId,
                        s.CreatedByUserId,
                        s.CreatedAtUtc
                    FROM AssetGroupShares s
                    INNER JOIN Assets a ON a.Id = s.AssetId
                    INNER JOIN Groups g ON g.Id = s.GroupId
                    WHERE a.WorkspaceId = @WorkspaceId
                      AND g.WorkspaceId = @WorkspaceId
                    ORDER BY s.CreatedAtUtc, s.AssetId, s.GroupId;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return shares.AsList();
        }

        public async Task<AssetVersion?> CreateAssetVersionAsync(int workspaceId, AssetVersion version, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetVersion>(
                new CommandDefinition(
                    """
                    INSERT INTO AssetVersions (
                        AssetId,
                        VersionNumber,
                        Name,
                        Description,
                        AssetType,
                        VersionLabel,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @AssetId,
                        @VersionNumber,
                        @Name,
                        @Description,
                        @AssetType,
                        @VersionLabel,
                        @CreatedByUserId,
                        @CreatedAtUtc
                    WHERE EXISTS (
                        SELECT 1
                        FROM Assets a
                        WHERE a.Id = @AssetId
                          AND a.WorkspaceId = @WorkspaceId
                    )
                    RETURNING
                        AssetId,
                        VersionNumber,
                        Name,
                        Description,
                        AssetType,
                        VersionLabel,
                        CreatedByUserId,
                        CreatedAtUtc;
                    """,
                    new
                    {
                        version.AssetId,
                        version.VersionNumber,
                        version.Name,
                        version.Description,
                        version.AssetType,
                        version.VersionLabel,
                        version.CreatedByUserId,
                        version.CreatedAtUtc,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<List<AssetVersion>> GetAssetVersionsAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var versions = await connection.QueryAsync<AssetVersion>(
                new CommandDefinition(
                    """
                    SELECT
                        v.AssetId,
                        v.VersionNumber,
                        v.Name,
                        v.Description,
                        v.AssetType,
                        v.VersionLabel,
                        v.CreatedByUserId,
                        v.CreatedAtUtc
                    FROM AssetVersions v
                    INNER JOIN Assets a ON a.Id = v.AssetId
                    WHERE v.AssetId = @AssetId
                      AND a.WorkspaceId = @WorkspaceId
                    ORDER BY v.VersionNumber DESC;
                    """,
                    new { AssetId = assetId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return versions.AsList();
        }

        public async Task<AssetVersion?> GetAssetVersionAsync(int workspaceId, int assetId, int versionNumber, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetVersion>(
                new CommandDefinition(
                    """
                    SELECT
                        v.AssetId,
                        v.VersionNumber,
                        v.Name,
                        v.Description,
                        v.AssetType,
                        v.VersionLabel,
                        v.CreatedByUserId,
                        v.CreatedAtUtc
                    FROM AssetVersions v
                    INNER JOIN Assets a ON a.Id = v.AssetId
                    WHERE v.AssetId = @AssetId
                      AND v.VersionNumber = @VersionNumber
                      AND a.WorkspaceId = @WorkspaceId
                    LIMIT 1;
                    """,
                    new { AssetId = assetId, VersionNumber = versionNumber, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdateAssetCurrentVersionAsync(int workspaceId, int assetId, int currentVersionNumber, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Assets
                    SET CurrentVersionNumber = @CurrentVersionNumber,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE Id = @AssetId
                      AND WorkspaceId = @WorkspaceId;
                    """,
                    new
                    {
                        AssetId = assetId,
                        WorkspaceId = workspaceId,
                        CurrentVersionNumber = currentVersionNumber,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));

            return updated > 0;
        }
    }
}
