using EduCollab.Application.Models;

using EduCollab.Application.Repositories;



namespace EduCollab.Application.Services.Assets

{

    public interface IAssetService

    {

        Task<bool> CreateAssetWithContentAsync(Asset asset, int groupId, string contentType, string? fileName, Stream content, CancellationToken cancellationToken);

        Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken);

        Task<List<Asset>> GetAssetsInGroupAsync(int groupId, CancellationToken cancellationToken);

        Task<List<Asset>> GetMyAssetsAsync(CancellationToken cancellationToken);

        Task<Asset?> GetAssetByIdAsync(int assetId, CancellationToken cancellationToken);

        Task<Asset?> UpdateAssetAsync(Asset asset, CancellationToken cancellationToken);

        Task<bool> DeleteAssetAsync(int assetId, CancellationToken cancellationToken);

        Task<bool> CanCurrentUserManageAssetAsync(int ownerUserId, CancellationToken cancellationToken);

        Task<AssetContent?> GetAssetContentAsync(int assetId, CancellationToken cancellationToken);

        Task SaveAssetContentAsync(int assetId, string contentType, string? fileName, Stream content, CancellationToken cancellationToken);

        Task<bool> CanCurrentUserViewAssetDirectlyAsync(int assetId, CancellationToken cancellationToken);

    }

}


