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
                    SELECT
                        @WorkspaceId,
                        @GroupId,
                        @OwnerUserId,
                        @Name,
                        @Description,
                        @AssetType,
                        @StorageUrl,
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
                    SELECT {AssetSelectColumns}
                    FROM Assets
                    WHERE WorkspaceId = @WorkspaceId
                      AND GroupId = @GroupId
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
                    SET GroupId = @GroupId,
                        Name = @Name,
                        Description = @Description,
                        AssetType = @AssetType,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE a.Id = @Id
                      AND a.WorkspaceId = @WorkspaceId
                      AND EXISTS (
                          SELECT 1
                          FROM Groups g
                          WHERE g.Id = @GroupId
                            AND g.WorkspaceId = @WorkspaceId
                      )
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
                        asset.GroupId,
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

        public async Task<Asset?> MoveAssetToGroupAsync(int workspaceId, int assetId, int groupId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleOrDefaultAsync<Asset>(
                new CommandDefinition(
                    $"""
                    UPDATE Assets AS a
                    SET GroupId = @GroupId,
                        UpdatedAtUtc = @UpdatedAtUtc
                    WHERE a.Id = @AssetId
                      AND a.WorkspaceId = @WorkspaceId
                      AND EXISTS (
                          SELECT 1
                          FROM Groups g
                          WHERE g.Id = @GroupId
                            AND g.WorkspaceId = @WorkspaceId
                      )
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
                        AssetId = assetId,
                        WorkspaceId = workspaceId,
                        GroupId = groupId,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
