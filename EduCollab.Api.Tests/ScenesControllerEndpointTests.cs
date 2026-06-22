using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests;

public sealed class ScenesControllerEndpointTests
{
    [Fact]
    public async Task UpdateScene_ReturnsBadRequest_WhenIfMatchHeaderIsMissing()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 51);

        var response = await client.PutAsJsonAsync("/api/workspace/scenes/10", new UpdateSceneRequest
        {
            Name = "Updated scene",
            JsonContent = "{}",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("precondition_required", body.Error);
    }

    [Fact]
    public async Task UpdateScene_ReturnsPreconditionFailed_WhenIfMatchIsStale()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.SceneService.UpdateSceneAsyncHandler = (_, ifMatch, _) =>
        {
            if (ifMatch != "current-etag")
            {
                throw new PreconditionFailedException("The scene was modified by another request.");
            }

            return Task.FromResult<Scene?>(null);
        };

        using var client = factory.CreateClient(userId: 52);
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "stale-etag");

        var response = await client.PutAsJsonAsync("/api/workspace/scenes/10", new UpdateSceneRequest
        {
            Name = "Updated scene",
            JsonContent = "{}",
        });

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("precondition_failed", body.Error);
    }

    [Fact]
    public async Task UpdateScene_ReturnsOk_AndNewETag_WhenIfMatchMatches()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.SceneService.UpdateSceneAsyncHandler = (scene, ifMatch, _) =>
        {
            Assert.Equal("current-etag", ifMatch);
            return Task.FromResult<Scene?>(new Scene
            {
                Id = scene.Id,
                WorkspaceId = 1,
                OwnerUserId = 53,
                Name = scene.Name,
                JsonContent = scene.JsonContent,
                ETag = "next-etag",
                CurrentVersionNumber = 2,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow,
            });
        };
        factory.SceneService.GetSceneGroupIdsAsyncHandler = (_, _) => Task.FromResult(new List<int>());

        using var client = factory.CreateClient(userId: 53);
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"current-etag\"");

        var response = await client.PutAsJsonAsync("/api/workspace/scenes/10", new UpdateSceneRequest
        {
            Name = "Updated scene",
            JsonContent = "{\"nodes\":[]}",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("ETag", out var updateEtags));
        Assert.Equal("next-etag", updateEtags.First());
        var body = await response.ReadAsJsonAsync<SceneResponse>();
        Assert.Equal("next-etag", body.ETag);
        Assert.Equal(2, body.CurrentVersionNumber);
    }

    [Fact]
    public async Task GetScene_ReturnsETagHeader()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.SceneService.GetSceneByIdAsyncHandler = (_, _, _) => Task.FromResult<Scene?>(new Scene
        {
            Id = 10,
            WorkspaceId = 1,
            OwnerUserId = 54,
            Name = "Scene",
            JsonContent = "{}",
            ETag = "scene-etag",
            CurrentVersionNumber = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        factory.SceneService.GetSceneGroupIdsAsyncHandler = (_, _) => Task.FromResult(new List<int>());

        using var client = factory.CreateClient(userId: 54);

        var response = await client.GetAsync("/api/workspace/scenes/10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("ETag", out var getEtags));
        Assert.Equal("scene-etag", getEtags.First());
    }
}
