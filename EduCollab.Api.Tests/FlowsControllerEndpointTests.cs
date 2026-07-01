using System.Net;
using EduCollab.Application.Models;
using EduCollab.Contracts.Responses.Flows;

namespace EduCollab.Api.Tests;

public sealed class FlowsControllerEndpointTests
{
    [Fact]
    public async Task GetFlows_ReturnsOk_WhenAuthenticated()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.FlowService.GetAllFlowsAsyncHandler = _ => Task.FromResult(new List<Flow>
        {
            new()
            {
                Id = 20,
                WorkspaceId = 1,
                OwnerUserId = 54,
                GroupId = 1,
                Name = "Intro flow",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            },
        });

        using var client = factory.CreateClient(userId: 54);

        var response = await client.GetAsync("/api/workspace/flows");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<FlowsResponse>();
        Assert.Single(body.Flows);
        Assert.Equal("Intro flow", body.Flows[0].Name);
    }
}
