using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface ISceneRepository
    {
        Task<int> CreateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken);
        Task<List<Scene>> GetAllScenesAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Scene>> GetScenesByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
        Task<List<Scene>> GetScenesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<Scene?> GetSceneByIdAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<Scene?> UpdateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<List<SceneAssetLink>> GetSceneAssetLinksAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<SceneAssetLink?> CreateSceneAssetLinkAsync(int workspaceId, SceneAssetLink link, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAssetLinkAsync(int workspaceId, int sceneId, int assetId, CancellationToken cancellationToken);
        Task<List<int>> GetSceneGroupIdsAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<Dictionary<int, List<int>>> GetSceneGroupIdsBySceneIdsAsync(int workspaceId, IReadOnlyCollection<int> sceneIds, CancellationToken cancellationToken);
        Task ReplaceSceneGroupSharesAsync(int workspaceId, int sceneId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task<bool> AddSceneGroupShareAsync(int workspaceId, int sceneId, int groupId, CancellationToken cancellationToken);
        Task<bool> RemoveSceneGroupShareAsync(int workspaceId, int sceneId, int groupId, CancellationToken cancellationToken);
        Task SyncScenePrimaryGroupIdAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
    }
}
