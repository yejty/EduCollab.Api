using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Users;

namespace EduCollab.Api.Tests;

public sealed class ApiProblemDetailsConsistencyTests
{
    [Fact]
    public async Task AuthorizedEndpoint_ReturnsUnauthorizedProblemDetails_WhenNotAuthenticated()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        response.AssertProblemJsonResponse();

        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal(401, body.Status);
        Assert.Equal("unauthorized", body.Error);
        Assert.Equal("Authentication is required for this operation.", body.Detail);
        Assert.Equal("urn:educollab:error:unauthorized", body.Type);
        Assert.False(string.IsNullOrWhiteSpace(body.RequestId));
    }

    [Fact]
    public async Task Register_ReturnsValidationProblemDetails_WhenPasswordIsWeak()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "password",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        response.AssertProblemJsonResponse();

        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("validation_failed", body.Error);
        Assert.Equal("urn:educollab:error:validation_failed", body.Type);
        Assert.Equal("One or more validation errors occurred.", body.Detail);
        Assert.False(string.IsNullOrWhiteSpace(body.RequestId));
        Assert.NotNull(body.Errors);
        Assert.True(body.Errors.ContainsKey("password"));
    }

    [Fact]
    public async Task Register_ReturnsValidationProblemDetails_WhenEmailIsInvalid()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example",
            Password = "Pass123!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        response.AssertProblemJsonResponse();

        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("validation_failed", body.Error);
        Assert.NotNull(body.Errors);
        Assert.True(body.Errors.ContainsKey("email"));
    }
}
