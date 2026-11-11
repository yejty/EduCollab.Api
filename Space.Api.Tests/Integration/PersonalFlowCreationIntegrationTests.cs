using System.Net;
using System.Net.Http.Json;
using Space.Contracts.Requests.Flows;
using Space.Contracts.Requests.Scenes;
using Space.Contracts.Responses.Flows;

namespace Space.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PersonalFlowCreationIntegrationTests
{
    [Fact]
    public async Task CreateFlow_ReturnsCreated_WhenGroupIdsOmitted()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var email = $"owner-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var tokens = await client.RegisterAndConfirmAsync(factory, "Owner User", email, password);
        client.SetBearerToken(tokens.AccessToken);

        await client.CreateApprovedWorkspaceAsync(
            factory,
            email,
            "Personal Flow Workspace",
            "Personal flow creation integration test");

        var createSceneResponse = await client.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Personal Scene",
            JsonContent = "{}",
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<Space.Contracts.Responses.Scenes.SceneResponse>();

        var createFlowResponse = await client.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Personal Flow",
            SceneIds = [scene.Id],
        });

        Assert.Equal(HttpStatusCode.Created, createFlowResponse.StatusCode);
        var body = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();
        Assert.Equal("Personal Flow", body.Name);
        Assert.Empty(body.GroupIds);
        Assert.Contains(scene.Id, body.SceneIds);
    }
}
