using EduCollab.Application.Models;



namespace EduCollab.Application.Services.Scenes

{

    public interface ISceneService

    {

        Task<bool> CreateSceneAsync(Scene scene, int groupId, CancellationToken cancellationToken);

        Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken);

        Task<List<Scene>> GetScenesInGroupAsync(int groupId, CancellationToken cancellationToken);

        Task<List<Scene>> GetMyScenesAsync(CancellationToken cancellationToken);

        Task<Scene?> GetSceneByIdAsync(int sceneId, CancellationToken cancellationToken);

        Task<Scene?> UpdateSceneAsync(Scene scene, CancellationToken cancellationToken);

        Task<bool> DeleteSceneAsync(int sceneId, CancellationToken cancellationToken);

        Task<bool> CanCurrentUserManageSceneAsync(int ownerUserId, CancellationToken cancellationToken);

        Task<List<SceneAssetContextItem>> GetSceneAssetsAsync(int sceneId, CancellationToken cancellationToken);

        Task<SceneAssetContextItem?> AttachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken);

        Task<bool> DetachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken);

    }

}


