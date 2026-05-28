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
    }
}
