using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Assets
{
    public interface IAssetService
    {
        Task<bool> CreateAssetAsync(Asset asset, int groupId, CancellationToken cancellationToken);
        Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsInFolderAsync(int folderId, CancellationToken cancellationToken);
        Task<List<Asset>> GetMyAssetsAsync(CancellationToken cancellationToken);
        Task<Asset?> GetAssetByIdAsync(int assetId, CancellationToken cancellationToken);
        Task<Asset?> UpdateAssetAsync(Asset asset, CancellationToken cancellationToken);
        Task<Asset?> MoveAssetAsync(int assetId, int? folderId, CancellationToken cancellationToken);
        Task<bool> DeleteAssetAsync(int assetId, CancellationToken cancellationToken);
        Task<bool> ShareAssetAsync(int assetId, int groupId, CancellationToken cancellationToken);
        Task<bool> RemoveAssetShareAsync(int assetId, int groupId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageAssetAsync(int ownerUserId, CancellationToken cancellationToken);
        Task<List<int>> GetAssetGroupIdsAsync(int assetId, CancellationToken cancellationToken);
        Task<AssetContent?> GetAssetContentAsync(int assetId, int? versionNumber, CancellationToken cancellationToken);
        Task SaveAssetContentAsync(int assetId, string contentType, string? fileName, Stream content, CancellationToken cancellationToken);
        Task<List<AssetVersion>> GetAssetVersionsAsync(int assetId, CancellationToken cancellationToken);
        Task<AssetVersion?> GetAssetVersionAsync(int assetId, int versionNumber, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserViewAssetDirectlyAsync(int assetId, CancellationToken cancellationToken);
    }
}
