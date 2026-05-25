using Dapper;
using EduCollab.Application.Models.Assets;
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
                        StorageProvider,
                        StorageKey,
                        MimeType,
                        SizeInBytes,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    SELECT
                        @WorkspaceId,
                        @FolderId,
                        @OwnerUserId,
                        @Name,
                        @Description,
                        @AssetType,
                        @StorageProvider,
                        @StorageKey,
                        @MimeType,
                        @SizeInBytes,
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
                        asset.StorageProvider,
                        asset.StorageKey,
                        asset.MimeType,
                        asset.SizeInBytes,
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
                        StorageProvider,
                        StorageKey,
                        MimeType,
                        SizeInBytes,
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
                        StorageProvider,
                        StorageKey,
                        MimeType,
                        SizeInBytes,
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
                        StorageProvider,
                        StorageKey,
                        MimeType,
                        SizeInBytes,
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
                        StorageProvider,
                        StorageKey,
                        MimeType,
                        SizeInBytes,
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
                        StorageProvider = @StorageProvider,
                        StorageKey = @StorageKey,
                        MimeType = @MimeType,
                        SizeInBytes = @SizeInBytes,
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
                        a.StorageProvider,
                        a.StorageKey,
                        a.MimeType,
                        a.SizeInBytes,
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
                        asset.StorageProvider,
                        asset.StorageKey,
                        asset.MimeType,
                        asset.SizeInBytes,
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
                        a.StorageProvider,
                        a.StorageKey,
                        a.MimeType,
                        a.SizeInBytes,
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
                        s.Role,
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

        public async Task<AssetGroupShare?> CreateAssetShareAsync(int workspaceId, AssetGroupShare share, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<AssetGroupShare>(
                new CommandDefinition(
                    """
                    INSERT INTO AssetGroupShares (
                        AssetId,
                        GroupId,
                        Role,
                        CreatedByUserId,
                        CreatedAtUtc)
                    SELECT
                        @AssetId,
                        @GroupId,
                        @Role,
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
                    RETURNING AssetId, GroupId, Role, CreatedByUserId, CreatedAtUtc;
                    """,
                    new
                    {
                        share.AssetId,
                        share.GroupId,
                        Role = share.Role.ToString(),
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
    }
}
