using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PersonalSceneCreationIntegrationTests
{
    [Fact]
    public async Task CreateScene_ReturnsCreated_WhenGroupIdsOmitted()
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
            "Personal Scene Workspace",
            "Personal scene creation integration test");

        var createSceneResponse = await client.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Personal Scene",
            JsonContent = JsonNode.Parse("""{"objects":[]}"""),
        });

        Assert.Equal(HttpStatusCode.Created, createSceneResponse.StatusCode);
        var body = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();
        Assert.Equal("Personal Scene", body.Name);
        Assert.Empty(body.GroupIds);
    }
}
