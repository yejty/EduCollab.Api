using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Assets
{
    public interface IAssetFolderService
    {
        Task<bool> CreateAssetFolderAsync(AssetFolder folder, CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetRootAssetFoldersAsync(CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetSubFoldersAsync(int folderId, CancellationToken cancellationToken);
        Task<AssetFolder?> GetAssetFolderByIdAsync(int folderId, CancellationToken cancellationToken);
        Task<AssetFolder?> UpdateAssetFolderAsync(AssetFolder folder, CancellationToken cancellationToken);
        Task<bool> DeleteAssetFolderAsync(int folderId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageWorkspaceAssetsAsync(CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetAllAssetFoldersAsync(CancellationToken cancellationToken);
    }
}
