using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Responses.Flows;

namespace EduCollab.Api.Tests;

public sealed class FlowScenesControllerEndpointTests
{
    [Fact]
    public async Task GetFlowScenes_returns_problem_details_when_flowId_missing()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 1);
        var response = await client.GetAsync("/api/workspace/flow-scenes");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_flow_id", body.Error);
    }

    [Fact]
    public async Task AttachFlowScene_returns_created_item()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.FlowService.AttachFlowSceneAsyncHandler = (flowId, sceneId, _) =>
            Task.FromResult<FlowSceneContextItem?>(new FlowSceneContextItem
            {
                FlowId = flowId,
                SceneId = sceneId,
                WorkspaceId = 1,
                Name = "Intro",
                GroupId = 5,
                UsableInFlow = true,
                CanViewDirectly = false,
                ResolvedFrom = FlowSceneResolvedFrom.FlowAttachment,
            });

        using var client = factory.CreateClient(userId: 1);
        var response = await client.PostAsJsonAsync(
            "/api/workspace/flow-scenes",
            new AttachFlowSceneRequest { FlowId = 10, SceneId = 501 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FlowSceneResponse>();
        Assert.Equal(501, body!.SceneId);
        Assert.Equal("FlowAttachment", body.ResolvedFrom);
    }

    [Fact]
    public async Task GetFlowSceneContent_returns_json_when_service_returns_content()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.FlowService.GetFlowSceneContentAsyncHandler = (_, _, _) =>
            Task.FromResult<string?>("{\"objects\":[]}");

        using var client = factory.CreateClient(userId: 1);
        var response = await client.GetAsync("/api/workspace/flow-scenes/content?flowId=10&sceneId=501");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("objects", body);
    }

    [Fact]
    public async Task GetFlowSceneContent_returns_not_found_when_content_unavailable()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 1);
        var response = await client.GetAsync("/api/workspace/flow-scenes/content?flowId=10&sceneId=501");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
