using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Assets
{
    public interface IAssetService
    {
        Task<bool> CreateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken);
        Task<List<Asset>> GetAllAssetsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Asset>> GetAssetsInFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<List<Asset>> GetMyAssetsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<Asset?> GetAssetByIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<Asset?> UpdateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken);
        Task<Asset?> MoveAssetAsync(int workspaceId, int assetId, int? folderId, CancellationToken cancellationToken);
        Task<bool> DeleteAssetAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageAssetAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
    }
}
