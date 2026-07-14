using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Responses.Flows;

namespace EduCollab.Api.Tests.Integration;

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

        var tokens = await client.RegisterAndConfirmAsync(factory, "Owner", "User", email, password);
        client.SetBearerToken(tokens.AccessToken);

        await client.CreateApprovedWorkspaceAsync(
            factory,
            email,
            "Personal Flow Workspace",
            "Personal flow creation integration test");

        var createFlowResponse = await client.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Personal Flow",
        });

        Assert.Equal(HttpStatusCode.Created, createFlowResponse.StatusCode);
        var body = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();
        Assert.Equal("Personal Flow", body.Name);
        Assert.Empty(body.GroupIds);
    }
}
