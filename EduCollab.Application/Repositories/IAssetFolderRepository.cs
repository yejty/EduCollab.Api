using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IAssetFolderRepository
    {
        Task<int> CreateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken);
        Task<List<AssetFolder>> GetAssetFoldersAsync(int workspaceId, int? parentFolderId, CancellationToken cancellationToken);
        Task<AssetFolder?> GetAssetFolderByIdAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<AssetFolder?> UpdateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken);
        Task UpdateDescendantPathsAsync(int workspaceId, string oldPathPrefix, string newPathPrefix, CancellationToken cancellationToken);
        Task<bool> DeleteAssetFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<List<AssetFolderGroupShare>> GetAssetFolderSharesAsync(int workspaceId, int folderId, CancellationToken cancellationToken);
        Task<AssetFolderGroupShare?> CreateAssetFolderShareAsync(int workspaceId, AssetFolderGroupShare share, CancellationToken cancellationToken);
        Task<bool> DeleteAssetFolderShareAsync(int workspaceId, int folderId, int groupId, CancellationToken cancellationToken);
    }
}
