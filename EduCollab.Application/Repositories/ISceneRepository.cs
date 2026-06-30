using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface ISceneRepository
    {
        Task<int> CreateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken);
        Task<List<Scene>> GetScenesByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<Scene?> GetSceneByIdAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<Scene?> UpdateSceneAsync(int workspaceId, Scene scene, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<List<SceneAssetLink>> GetSceneAssetLinksAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
        Task<SceneAssetLink?> CreateSceneAssetLinkAsync(int workspaceId, SceneAssetLink link, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAssetLinkAsync(int workspaceId, int sceneId, int assetId, CancellationToken cancellationToken);
    }
}
