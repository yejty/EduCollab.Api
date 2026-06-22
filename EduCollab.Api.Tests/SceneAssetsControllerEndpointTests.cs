using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests;

public class SceneAssetsControllerEndpointTests
{
    [Fact]
    public async Task GetSceneAssets_returns_problem_details_when_sceneId_missing()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 1);
        var response = await client.GetAsync("/api/workspace/scene-assets");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_scene_id", body.Error);
    }

    [Fact]
    public async Task AttachSceneAsset_returns_created_item()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.SceneService.AttachSceneAssetAsyncHandler = (_, assetId, _) =>
            Task.FromResult<SceneAssetContextItem?>(new SceneAssetContextItem
            {
                SceneId = 10,
                AssetId = assetId,
                WorkspaceId = 1,
                Name = "Chair",
                AssetType = "Model",
                UsableInScene = true,
                CanViewDirectly = false,
                ResolvedFrom = SceneAssetResolvedFrom.SceneAttachment,
            });

        using var client = factory.CreateClient(userId: 1);
        var response = await client.PostAsJsonAsync(
            "/api/workspace/scene-assets",
            new AttachSceneAssetRequest { SceneId = 10, AssetId = 501 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SceneAssetResponse>();
        Assert.Equal(501, body!.AssetId);
        Assert.Equal("SceneAttachment", body.ResolvedFrom);
    }
}
