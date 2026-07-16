using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Scenes;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeSceneService : ISceneService
{
    public Func<Scene, IReadOnlyList<int>, CancellationToken, Task<bool>>? CreateSceneAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Scene>>>? GetAllScenesAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Scene>>>? GetMyScenesAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<Scene>>>? GetScenesInGroupAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<Scene?>>? GetSceneByIdAsyncHandler { get; set; }
    public Func<Scene, CancellationToken, Task<Scene?>>? UpdateSceneAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? DeleteSceneAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? CanCurrentUserManageSceneAsyncHandler { get; set; }

    public Task<bool> CreateSceneAsync(Scene scene, IReadOnlyList<int> groupIds, CancellationToken cancellationToken) =>
        CreateSceneAsyncHandler?.Invoke(scene, groupIds, cancellationToken) ?? Task.FromResult(true);

    public Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken) =>
        GetAllScenesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Scene>());

    public Task<List<Scene>> GetMyScenesAsync(CancellationToken cancellationToken) =>
        GetMyScenesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Scene>());

    public Task<List<Scene>> GetScenesInGroupAsync(int groupId, CancellationToken cancellationToken) =>
        GetScenesInGroupAsyncHandler?.Invoke(groupId, cancellationToken) ?? Task.FromResult(new List<Scene>());

    public Task<Scene?> GetSceneByIdAsync(int sceneId, CancellationToken cancellationToken) =>
        GetSceneByIdAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult<Scene?>(null);

    public Task<Scene?> UpdateSceneAsync(Scene scene, CancellationToken cancellationToken) =>
        UpdateSceneAsyncHandler?.Invoke(scene, cancellationToken) ?? Task.FromResult<Scene?>(null);

    public Task<bool> DeleteSceneAsync(int sceneId, CancellationToken cancellationToken) =>
        DeleteSceneAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult(false);

    public Task<bool> CanCurrentUserManageSceneAsync(int ownerUserId, CancellationToken cancellationToken) =>
        CanCurrentUserManageSceneAsyncHandler?.Invoke(ownerUserId, cancellationToken) ?? Task.FromResult(true);

    public Task<SceneAssetsManifest> GetSceneAssetsAsync(int sceneId, int? flowId, CancellationToken cancellationToken) =>
        Task.FromResult(new SceneAssetsManifest { SceneId = sceneId });

    public Task<AssetContent?> GetSceneAssetContentAsync(string downloadToken, CancellationToken cancellationToken) =>
        Task.FromResult<AssetContent?>(null);

    public Task<List<SceneGroupShare>> GetSceneGroupSharesAsync(int sceneId, CancellationToken cancellationToken) =>
        Task.FromResult(new List<SceneGroupShare>());

    public Task<List<SceneGroupShare>?> SetSceneGroupSharesAsync(
        int sceneId,
        IReadOnlyList<SceneGroupShare> shares,
        CancellationToken cancellationToken) =>
        Task.FromResult<List<SceneGroupShare>?>(shares.ToList());

    public Task<bool> AddSceneGroupAsync(int sceneId, int groupId, bool includeAssets, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<bool> RemoveSceneGroupAsync(int sceneId, int groupId, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}
