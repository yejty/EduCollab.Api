using EduCollab.Application.Models.Assets;

namespace EduCollab.Application.Services.Assets
{
    public interface IAssetFolderService
    {
        Task<bool> CreateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetRootAssetFoldersAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetSubFoldersAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<AssetFolder?> GetAssetFolderByIdAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<AssetFolder?> UpdateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken);
        Task<bool> DeleteAssetFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageWorkspaceAssetsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetAllAssetFoldersAsync(int workspaceId, CancellationToken cancellationToken);
    }
}
