using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IAssetRepository
    {
        Task<int> CreateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken);
        Task<List<Asset>> GetAllAssetsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
        Task<Asset?> GetAssetByIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<Asset?> UpdateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken);
        Task UpdateAssetStorageUrlAsync(int workspaceId, int assetId, string storageUrl, CancellationToken cancellationToken);
        Task<bool> DeleteAssetAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<List<int>> GetAssetGroupIdsAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<Dictionary<int, List<int>>> GetAssetGroupIdsByAssetIdsAsync(int workspaceId, IReadOnlyCollection<int> assetIds, CancellationToken cancellationToken);
        Task ReplaceAssetGroupSharesAsync(int workspaceId, int assetId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task<bool> AddAssetGroupShareAsync(int workspaceId, int assetId, int groupId, CancellationToken cancellationToken);
        Task<bool> RemoveAssetGroupShareAsync(int workspaceId, int assetId, int groupId, CancellationToken cancellationToken);
        Task SyncAssetPrimaryGroupIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
    }
}
