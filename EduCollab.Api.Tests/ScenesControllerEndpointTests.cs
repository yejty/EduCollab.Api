using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests;

public sealed class ScenesControllerEndpointTests
{
    [Fact]
    public async Task UpdateScene_ReturnsOk_WhenSceneExists()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.SceneService.UpdateSceneAsyncHandler = (scene, _) =>
            Task.FromResult<Scene?>(new Scene
            {
                Id = scene.Id,
                WorkspaceId = 1,
                OwnerUserId = 53,
                GroupId = scene.GroupId,
                Name = scene.Name,
                JsonContent = scene.JsonContent,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow,
            });

        using var client = factory.CreateClient(userId: 53);

        var response = await client.PutAsJsonAsync("/api/workspace/scenes/10", new UpdateSceneRequest
        {
            Name = "Updated scene",
            GroupId = 1,
            JsonContent = "{\"nodes\":[]}",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<SceneResponse>();
        Assert.Equal("Updated scene", body.Name);
    }

    [Fact]
    public async Task GetScene_ReturnsScene_WhenAuthenticated()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.SceneService.GetSceneByIdAsyncHandler = (_, _) => Task.FromResult<Scene?>(new Scene
        {
            Id = 10,
            WorkspaceId = 1,
            OwnerUserId = 54,
            GroupId = 1,
            Name = "Scene",
            JsonContent = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });

        using var client = factory.CreateClient(userId: 54);

        var response = await client.GetAsync("/api/workspace/scenes/10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<SceneResponse>();
        Assert.Equal("Scene", body.Name);
    }
}
