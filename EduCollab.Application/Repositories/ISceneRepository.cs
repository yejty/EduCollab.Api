using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface ISceneRepository
    {
        Task<int> CreateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken);
        Task<List<Scene>> GetAllScenesAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Scene>> GetScenesByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
        Task<Scene?> GetSceneByIdAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<Scene?> UpdateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<List<SceneGroupShare>> GetSceneSharesAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<List<SceneGroupShare>> GetWorkspaceSceneSharesAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<SceneGroupShare>> GetSceneSharesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<SceneGroupShare?> CreateSceneShareAsync(int workspaceId, SceneGroupShare share, CancellationToken cancellationToken);
        Task<bool> DeleteSceneShareAsync(int workspaceId, int sceneId, int groupId, CancellationToken cancellationToken);
        Task<SceneVersion?> CreateSceneVersionAsync(int workspaceId, SceneVersion version, CancellationToken cancellationToken);
        Task<List<SceneVersion>> GetSceneVersionsAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<SceneVersion?> GetSceneVersionAsync(int workspaceId, int sceneId, int versionNumber, CancellationToken cancellationToken);
        Task<bool> UpdateSceneCurrentVersionAsync(int workspaceId, int sceneId, int currentVersionNumber, CancellationToken cancellationToken);
        Task<List<SceneAssetLink>> GetSceneAssetLinksAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<SceneAssetLink?> CreateSceneAssetLinkAsync(int workspaceId, SceneAssetLink link, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAssetLinkAsync(int workspaceId, int sceneId, int assetId, CancellationToken cancellationToken);
    }
}
