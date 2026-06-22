using EduCollab.Application.Models;
using EduCollab.Application.Services.Scenes;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeSceneService : ISceneService
{
    public Func<Scene, int, CancellationToken, Task<bool>>? CreateSceneAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Scene>>>? GetAllScenesAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Scene>>>? GetMyScenesAsyncHandler { get; set; }
    public Func<int, int?, CancellationToken, Task<Scene?>>? GetSceneByIdAsyncHandler { get; set; }
    public Func<Scene, string, CancellationToken, Task<Scene?>>? UpdateSceneAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? DeleteSceneAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<bool>>? ShareSceneAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<bool>>? RemoveSceneShareAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<int>>>? GetSceneGroupIdsAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? CanCurrentUserManageSceneAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<SceneVersion>>>? GetSceneVersionsAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<SceneVersion?>>? GetSceneVersionAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<SceneAssetContextItem>>>? GetSceneAssetsAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<SceneAssetContextItem?>>? AttachSceneAssetAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<bool>>? DetachSceneAssetAsyncHandler { get; set; }

    public Task<bool> CreateSceneAsync(Scene scene, int groupId, CancellationToken cancellationToken) =>
        CreateSceneAsyncHandler?.Invoke(scene, groupId, cancellationToken) ?? Task.FromResult(true);

    public Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken) =>
        GetAllScenesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Scene>());

    public Task<List<Scene>> GetMyScenesAsync(CancellationToken cancellationToken) =>
        GetMyScenesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Scene>());

    public Task<Scene?> GetSceneByIdAsync(int sceneId, int? versionNumber, CancellationToken cancellationToken) =>
        GetSceneByIdAsyncHandler?.Invoke(sceneId, versionNumber, cancellationToken) ?? Task.FromResult<Scene?>(null);

    public Task<Scene?> UpdateSceneAsync(Scene scene, string ifMatch, CancellationToken cancellationToken) =>
        UpdateSceneAsyncHandler?.Invoke(scene, ifMatch, cancellationToken) ?? Task.FromResult<Scene?>(null);

    public Task<bool> DeleteSceneAsync(int sceneId, CancellationToken cancellationToken) =>
        DeleteSceneAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult(false);

    public Task<bool> ShareSceneAsync(int sceneId, int groupId, CancellationToken cancellationToken) =>
        ShareSceneAsyncHandler?.Invoke(sceneId, groupId, cancellationToken) ?? Task.FromResult(false);

    public Task<bool> RemoveSceneShareAsync(int sceneId, int groupId, CancellationToken cancellationToken) =>
        RemoveSceneShareAsyncHandler?.Invoke(sceneId, groupId, cancellationToken) ?? Task.FromResult(false);

    public Task<List<int>> GetSceneGroupIdsAsync(int sceneId, CancellationToken cancellationToken) =>
        GetSceneGroupIdsAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult(new List<int>());

    public Task<bool> CanCurrentUserManageSceneAsync(int ownerUserId, CancellationToken cancellationToken) =>
        CanCurrentUserManageSceneAsyncHandler?.Invoke(ownerUserId, cancellationToken) ?? Task.FromResult(true);

    public Task<List<SceneVersion>> GetSceneVersionsAsync(int sceneId, CancellationToken cancellationToken) =>
        GetSceneVersionsAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult(new List<SceneVersion>());

    public Task<SceneVersion?> GetSceneVersionAsync(int sceneId, int versionNumber, CancellationToken cancellationToken) =>
        GetSceneVersionAsyncHandler?.Invoke(sceneId, versionNumber, cancellationToken) ?? Task.FromResult<SceneVersion?>(null);

    public Task<List<SceneAssetContextItem>> GetSceneAssetsAsync(int sceneId, CancellationToken cancellationToken) =>
        GetSceneAssetsAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult(new List<SceneAssetContextItem>());

    public Task<SceneAssetContextItem?> AttachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken) =>
        AttachSceneAssetAsyncHandler?.Invoke(sceneId, assetId, cancellationToken) ?? Task.FromResult<SceneAssetContextItem?>(null);

    public Task<bool> DetachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken) =>
        DetachSceneAssetAsyncHandler?.Invoke(sceneId, assetId, cancellationToken) ?? Task.FromResult(false);
}
