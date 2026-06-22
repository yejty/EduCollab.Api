using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduCollab.Api.Tests;

internal static class TestHttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    public static HttpClient CreateClient(this ApiWebApplicationFactory factory, int? userId = null, string? email = null)
    {
        var client = factory.CreateClient();
        if (userId is int id)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, id.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email ?? $"user{id}@example.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        }

        return client;
    }

    public static async Task<T> ReadAsJsonAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return value ?? throw new InvalidOperationException($"Response body could not be read as {typeof(T).Name}.");
    }

    public static void AssertProblemJsonResponse(this HttpResponseMessage response)
    {
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.Contains("X-Request-Id"));
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues("X-Request-Id").First()));
    }
}
