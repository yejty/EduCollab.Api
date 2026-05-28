using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Assets
{
    public interface IAssetService
    {
        Task<bool> CreateAssetAsync(Asset asset, CancellationToken cancellationToken);
        Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsInFolderAsync(int folderId, CancellationToken cancellationToken);
        Task<List<Asset>> GetMyAssetsAsync(CancellationToken cancellationToken);
        Task<Asset?> GetAssetByIdAsync(int assetId, CancellationToken cancellationToken);
        Task<Asset?> UpdateAssetAsync(Asset asset, CancellationToken cancellationToken);
        Task<Asset?> MoveAssetAsync(int assetId, int? folderId, CancellationToken cancellationToken);
        Task<bool> DeleteAssetAsync(int assetId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageAssetAsync(int ownerUserId, CancellationToken cancellationToken);
        Task<List<int>> GetAssetGroupIdsAsync(int assetId, CancellationToken cancellationToken);
    }
}
