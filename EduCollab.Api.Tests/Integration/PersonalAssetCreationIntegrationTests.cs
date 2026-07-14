using System.Net;
using EduCollab.Contracts.Responses.Assets;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PersonalAssetCreationIntegrationTests
{
    [Fact]
    public async Task CreateAsset_ReturnsCreated_WhenGroupIdsOmitted()
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
            "Personal Asset Workspace",
            "Personal asset creation integration test");

        using var form = AssetTestHelpers.CreateAssetMultipartForm("Personal Chair");
        var response = await client.PostAsync("/api/workspace/assets", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadAsJsonAsync<AssetResponse>();
        Assert.Equal("Personal Chair", body.Name);
        Assert.Empty(body.GroupIds);
    }
}
