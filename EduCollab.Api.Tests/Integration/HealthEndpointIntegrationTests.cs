using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Responses;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HealthEndpointIntegrationTests
{
    [Fact]
    public async Task GetHealth_ReturnsHealthy_WhenDatabaseIsAvailable()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Equal("Healthy", body.Checks["Database"]);
    }
}
