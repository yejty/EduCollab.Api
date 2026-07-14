using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Scenes
{
    public interface ISceneService
    {
        Task<bool> CreateSceneAsync(Scene scene, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken);
        Task<List<Scene>> GetMyScenesAsync(CancellationToken cancellationToken);
        Task<List<Scene>> GetScenesInGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<Scene?> GetSceneByIdAsync(int sceneId, CancellationToken cancellationToken);
        Task<Scene?> UpdateSceneAsync(Scene scene, CancellationToken cancellationToken);
        Task<bool> DeleteSceneAsync(int sceneId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageSceneAsync(int ownerUserId, CancellationToken cancellationToken);
        Task<List<int>> GetSceneGroupIdsAsync(int sceneId, CancellationToken cancellationToken);
        Task<List<int>?> SetSceneGroupIdsAsync(int sceneId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task<bool> AddSceneGroupAsync(int sceneId, int groupId, CancellationToken cancellationToken);
        Task<bool> RemoveSceneGroupAsync(int sceneId, int groupId, CancellationToken cancellationToken);
    }
}
