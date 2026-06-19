using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IAssetRepository
    {
        Task<int> CreateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken);
        Task<List<Asset>> GetAllAssetsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsByFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
        Task<Asset?> GetAssetByIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<Asset?> UpdateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken);
        Task UpdateAssetStorageUrlAsync(int workspaceId, int assetId, string storageUrl, CancellationToken cancellationToken);
        Task<bool> DeleteAssetAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<Asset?> MoveAssetAsync(int workspaceId, int assetId, int? folderId, CancellationToken cancellationToken);
        Task<List<AssetGroupShare>> GetAssetSharesAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<List<AssetGroupShare>> GetWorkspaceAssetSharesAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<AssetGroupShare>> GetAssetSharesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<AssetGroupShare?> CreateAssetShareAsync(int workspaceId, AssetGroupShare share, CancellationToken cancellationToken);
        Task<bool> DeleteAssetShareAsync(int workspaceId, int assetId, int groupId, CancellationToken cancellationToken);
        Task<AssetVersion?> CreateAssetVersionAsync(int workspaceId, AssetVersion version, CancellationToken cancellationToken);
        Task<List<AssetVersion>> GetAssetVersionsAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<AssetVersion?> GetAssetVersionAsync(int workspaceId, int assetId, int versionNumber, CancellationToken cancellationToken);
        Task<bool> UpdateAssetCurrentVersionAsync(int workspaceId, int assetId, int currentVersionNumber, CancellationToken cancellationToken);
    }
}
