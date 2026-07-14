using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Responses;

namespace EduCollab.Api.Tests;

public sealed class HealthControllerEndpointTests
{
    [Fact]
    public async Task GetHealth_DoesNotRequireAuthentication()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Status));
        Assert.Contains("Database", body.Checks.Keys);
    }
}
