using EduCollab.Application.Models;

using EduCollab.Application.Repositories;



namespace EduCollab.Application.Services.Assets

{

    public interface IAssetService

    {

        Task<bool> CreateAssetWithContentAsync(
            Asset asset,
            IReadOnlyList<int> groupIds,
            string contentType,
            string? fileName,
            Stream content,
            CancellationToken cancellationToken);

        Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken);

        Task<List<Asset>> GetAssetsInGroupAsync(int groupId, CancellationToken cancellationToken);

        Task<List<Asset>> GetMyAssetsAsync(CancellationToken cancellationToken);

        Task<Asset?> GetAssetByIdAsync(int assetId, CancellationToken cancellationToken);

        Task<Asset?> UpdateAssetAsync(Asset asset, IReadOnlyList<int>? groupIds, CancellationToken cancellationToken);

        Task<bool> DeleteAssetAsync(int assetId, CancellationToken cancellationToken);

        Task<bool> CanCurrentUserManageAssetAsync(int ownerUserId, CancellationToken cancellationToken);

        Task<AssetContent?> GetAssetContentAsync(int assetId, CancellationToken cancellationToken);

        Task SaveAssetContentAsync(int assetId, string contentType, string? fileName, Stream content, CancellationToken cancellationToken);

        Task<bool> CanCurrentUserViewAssetDirectlyAsync(int assetId, CancellationToken cancellationToken);

        Task<List<int>> GetAssetGroupIdsAsync(int assetId, CancellationToken cancellationToken);

        Task<List<int>?> SetAssetGroupIdsAsync(int assetId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);

        Task<bool> AddAssetGroupAsync(int assetId, int groupId, CancellationToken cancellationToken);

        Task<bool> RemoveAssetGroupAsync(int assetId, int groupId, CancellationToken cancellationToken);

    }

}

