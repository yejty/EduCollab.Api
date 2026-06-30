using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Scenes;



namespace EduCollab.Api.Tests.Fakes;



public sealed class FakeSceneService : ISceneService

{

    public Func<Scene, int, CancellationToken, Task<bool>>? CreateSceneAsyncHandler { get; set; }

    public Func<int, CancellationToken, Task<List<Scene>>>? GetScenesInGroupAsyncHandler { get; set; }

    public Func<int, CancellationToken, Task<Scene?>>? GetSceneByIdAsyncHandler { get; set; }

    public Func<Scene, CancellationToken, Task<Scene?>>? UpdateSceneAsyncHandler { get; set; }

    public Func<int, CancellationToken, Task<bool>>? DeleteSceneAsyncHandler { get; set; }

    public Func<int, CancellationToken, Task<bool>>? CanCurrentUserManageSceneAsyncHandler { get; set; }

    public Func<int, CancellationToken, Task<List<SceneAssetContextItem>>>? GetSceneAssetsAsyncHandler { get; set; }

    public Func<int, int, CancellationToken, Task<SceneAssetContextItem?>>? AttachSceneAssetAsyncHandler { get; set; }

    public Func<int, int, CancellationToken, Task<bool>>? DetachSceneAssetAsyncHandler { get; set; }

    public Func<int, int, CancellationToken, Task<AssetContent?>>? GetSceneAssetContentAsyncHandler { get; set; }



    public Task<bool> CreateSceneAsync(Scene scene, int groupId, CancellationToken cancellationToken) =>

        CreateSceneAsyncHandler?.Invoke(scene, groupId, cancellationToken) ?? Task.FromResult(true);



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



    public Task<List<SceneAssetContextItem>> GetSceneAssetsAsync(int sceneId, CancellationToken cancellationToken) =>

        GetSceneAssetsAsyncHandler?.Invoke(sceneId, cancellationToken) ?? Task.FromResult(new List<SceneAssetContextItem>());



    public Task<SceneAssetContextItem?> AttachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken) =>

        AttachSceneAssetAsyncHandler?.Invoke(sceneId, assetId, cancellationToken) ?? Task.FromResult<SceneAssetContextItem?>(null);



    public Task<bool> DetachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken) =>

        DetachSceneAssetAsyncHandler?.Invoke(sceneId, assetId, cancellationToken) ?? Task.FromResult(false);



    public Task<AssetContent?> GetSceneAssetContentAsync(int sceneId, int assetId, CancellationToken cancellationToken) =>

        GetSceneAssetContentAsyncHandler?.Invoke(sceneId, assetId, cancellationToken) ?? Task.FromResult<AssetContent?>(null);

}


