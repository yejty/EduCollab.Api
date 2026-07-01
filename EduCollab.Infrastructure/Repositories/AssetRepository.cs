using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories
{
    public class AssetRepository : IAssetRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string AssetSelectColumns =
            """
            Id,
            WorkspaceId,
            GroupId,
            OwnerUserId,
            Name,
            Description,
            AssetType,
            StorageUrl,
            CreatedAtUtc,
            UpdatedAtUtc
            """;

        public AssetRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assetId = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    $"""
                    INSERT INTO Assets (
                        WorkspaceId,
                        GroupId,
                        OwnerUserId,
                        Name,
                        Description,
                        AssetType,
                        StorageUrl,
                        CreatedAtUtc,
                        UpdatedAtUtc)
                    VALUES (
                        @WorkspaceId,
                        @GroupId,
                        @OwnerUserId,
                        @Name,
                        @Description,
                        @AssetType,
                        @StorageUrl,
                        @CreatedAtUtc,
                        @UpdatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        WorkspaceId = workspaceId,
                        asset.GroupId,
                        asset.OwnerUserId,
                        asset.Name,
                        asset.Description,
                        asset.AssetType,
                        asset.StorageUrl,
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
                    $"""
                    SELECT {AssetSelectColumns}
                    FROM Assets
                    WHERE WorkspaceId = @WorkspaceId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return assets.AsList();
        }

        public async Task<List<Asset>> GetAssetsByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assets = await connection.QueryAsync<Asset>(
                new CommandDefinition(
                    $"""
                    SELECT DISTINCT {AssetSelectColumns}
                    FROM Assets a
                    INNER JOIN AssetGroupShares ags ON ags.AssetId = a.Id
                    WHERE a.WorkspaceId = @WorkspaceId
                      AND ags.GroupId = @GroupId
                    ORDER BY Name ASC, Id ASC;
                    """,
                    new { WorkspaceId = workspaceId, GroupId = groupId },
                    cancellationToken: cancellationToken));

            return assets.AsList();
        }

        public async Task<List<Asset>> GetAssetsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var assets = await connection.QueryAsync<Asset>(
                new CommandDefinition(
                    $"""
                    SELECT {AssetSelectColumns}
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
                    $"""
                    SELECT {AssetSelectColumns}
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
                    $"""
                    UPDATE Assets AS a
                    SET Name = @Name,
                        Description = @Description,
                        AssetType = @AssetType,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE a.Id = @Id
                      AND a.WorkspaceId = @WorkspaceId
                    RETURNING
                        a.Id,
                        a.WorkspaceId,
                        a.GroupId,
                        a.OwnerUserId,
                        a.Name,
                        a.Description,
                        a.AssetType,
                        a.StorageUrl,
                        a.CreatedAtUtc,
                        a.UpdatedAtUtc;
                    """,
                    new
                    {
                        asset.Id,
                        WorkspaceId = workspaceId,
                        asset.Name,
                        asset.Description,
                        asset.AssetType,
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

        public async Task<List<int>> GetAssetGroupIdsAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var groupIds = await connection.QueryAsync<int>(
                new CommandDefinition(
                    """
                    SELECT ags.GroupId
                    FROM AssetGroupShares ags
                    INNER JOIN Assets a ON a.Id = ags.AssetId
                    WHERE ags.AssetId = @AssetId
                      AND a.WorkspaceId = @WorkspaceId
                    ORDER BY ags.CreatedAtUtc, ags.GroupId;
                    """,
                    new { AssetId = assetId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return groupIds.AsList();
        }

        public async Task<Dictionary<int, List<int>>> GetAssetGroupIdsByAssetIdsAsync(
            int workspaceId,
            IReadOnlyCollection<int> assetIds,
            CancellationToken cancellationToken)
        {
            if (assetIds.Count == 0)
                return new Dictionary<int, List<int>>();

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var rows = await connection.QueryAsync<(int AssetId, int GroupId)>(
                new CommandDefinition(
                    """
                    SELECT ags.AssetId, ags.GroupId
                    FROM AssetGroupShares ags
                    INNER JOIN Assets a ON a.Id = ags.AssetId
                    WHERE a.WorkspaceId = @WorkspaceId
                      AND ags.AssetId = ANY(@AssetIds)
                    ORDER BY ags.AssetId, ags.CreatedAtUtc, ags.GroupId;
                    """,
                    new { WorkspaceId = workspaceId, AssetIds = assetIds.ToArray() },
                    cancellationToken: cancellationToken));

            return rows
                .GroupBy(row => row.AssetId)
                .ToDictionary(group => group.Key, group => group.Select(row => row.GroupId).ToList());
        }

        public async Task ReplaceAssetGroupSharesAsync(
            int workspaceId,
            int assetId,
            IReadOnlyList<int> groupIds,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM AssetGroupShares ags
                    USING Assets a
                    WHERE ags.AssetId = @AssetId
                      AND a.Id = ags.AssetId
                      AND a.WorkspaceId = @WorkspaceId;
                    """,
                    new { AssetId = assetId, WorkspaceId = workspaceId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var createdAtUtc = DateTime.UtcNow;
            foreach (var groupId in groupIds.Distinct())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO AssetGroupShares (AssetId, GroupId, CreatedAtUtc)
                        SELECT @AssetId, @GroupId, @CreatedAtUtc
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
                        );
                        """,
                        new
                        {
                            AssetId = assetId,
                            GroupId = groupId,
                            CreatedAtUtc = createdAtUtc,
                            WorkspaceId = workspaceId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }

            transaction.Commit();
        }

        public async Task<bool> AddAssetGroupShareAsync(
            int workspaceId,
            int assetId,
            int groupId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO AssetGroupShares (AssetId, GroupId, CreatedAtUtc)
                    SELECT @AssetId, @GroupId, @CreatedAtUtc
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
                    ON CONFLICT (AssetId, GroupId) DO NOTHING;
                    """,
                    new
                    {
                        AssetId = assetId,
                        GroupId = groupId,
                        CreatedAtUtc = DateTime.UtcNow,
                        WorkspaceId = workspaceId
                    },
                    cancellationToken: cancellationToken));

            return inserted > 0;
        }

        public async Task<bool> RemoveAssetGroupShareAsync(
            int workspaceId,
            int assetId,
            int groupId,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM AssetGroupShares ags
                    USING Assets a
                    WHERE ags.AssetId = @AssetId
                      AND ags.GroupId = @GroupId
                      AND a.Id = ags.AssetId
                      AND a.WorkspaceId = @WorkspaceId;
                    """,
                    new { AssetId = assetId, GroupId = groupId, WorkspaceId = workspaceId },
                    cancellationToken: cancellationToken));

            return deleted > 0;
        }

        public async Task SyncAssetPrimaryGroupIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Assets a
                    SET GroupId = (
                        SELECT ags.GroupId
                        FROM AssetGroupShares ags
                        WHERE ags.AssetId = a.Id
                        ORDER BY ags.CreatedAtUtc, ags.GroupId
                        LIMIT 1
                    ),
                    UpdatedAtUtc = @UpdatedAtUtc
                    WHERE a.Id = @AssetId
                      AND a.WorkspaceId = @WorkspaceId;
                    """,
                    new
                    {
                        AssetId = assetId,
                        WorkspaceId = workspaceId,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
