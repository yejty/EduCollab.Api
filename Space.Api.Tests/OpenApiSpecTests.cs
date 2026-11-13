using System.Text.Json;
using System.Text.Json.Nodes;

namespace Space.Api.Tests;

public sealed class OpenApiSpecTests
{
    [Fact]
    public async Task LiveSwaggerDocument_isAvailable()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hub API", document?["info"]?["title"]?.GetValue<string>());
        Assert.Equal("1.0", document?["info"]?["version"]?.GetValue<string>());
        Assert.NotNull(document?["paths"]);
        Assert.True(document!["paths"]!.AsObject().Count > 0);
        Assert.NotNull(document["paths"]?["/api/users/login"]);
        Assert.Null(document["paths"]?["/api/workspace/assets"]);
    }

    [Fact]
    public async Task WorkspaceOperations_documentRequiredParameters()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var createInvite = document?["paths"]?["/api/workspace/invitations"]?["post"];
        Assert.NotNull(createInvite);
        Assert.NotNull(createInvite["x-workspace-parameters"]);

        var parameters = createInvite["x-workspace-parameters"]?["parameters"]?.AsArray();
        Assert.NotNull(parameters);
        Assert.Contains(parameters, parameter => parameter?.GetValue<string>() == "inviteUsers");

        var description = createInvite["description"]?.GetValue<string>();
        Assert.NotNull(description);
        Assert.Contains("Workspace parameters", description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommittedOpenApiSpec_matchesLiveSwaggerDocument()
    {
        var specPath = ResolveCommittedSpecPath();
        Assert.True(File.Exists(specPath), $"Committed OpenAPI spec not found at {specPath}. Run scripts/export-openapi.ps1.");

        var expectedJson = await File.ReadAllTextAsync(specPath);

        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var actualJson = await response.Content.ReadAsStringAsync();

        Assert.True(
            JsonSemanticEquals(expectedJson, actualJson),
            "Committed docs/hub.openapi.json is out of date. Run scripts/export-openapi.ps1 and commit the result.");
    }

    [Fact]
    public async Task ExportCommittedOpenApiSpec_WhenExportFlagIsSet()
    {
        if (Environment.GetEnvironmentVariable("EXPORT_OPENAPI") != "1")
        {
            return;
        }

        await OpenApiSpecExporter.ExportCommittedSpecAsync();
    }

    public static string ResolveCommittedSpecPath()
    {
        return Path.Combine(ResolveRepoRoot(), "docs", "hub.openapi.json");
    }

    public static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
               && !File.Exists(Path.Combine(directory.FullName, "Space.Api.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    internal static bool JsonSemanticEqualsPublic(string expectedJson, string actualJson) =>
        JsonSemanticEquals(expectedJson, actualJson);

    private static bool JsonSemanticEquals(string expectedJson, string actualJson)
    {
        using var expected = JsonDocument.Parse(expectedJson);
        using var actual = JsonDocument.Parse(actualJson);
        return JsonElementDeepEquals(expected.RootElement, actual.RootElement);
    }

    private static bool JsonElementDeepEquals(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            return false;
        }

        return expected.ValueKind switch
        {
            JsonValueKind.Object => JsonObjectDeepEquals(expected, actual),
            JsonValueKind.Array => JsonArrayDeepEquals(expected, actual),
            JsonValueKind.String => expected.GetString() == actual.GetString(),
            JsonValueKind.Number => expected.GetRawText() == actual.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => expected.GetBoolean() == actual.GetBoolean(),
            JsonValueKind.Null => true,
            _ => expected.GetRawText() == actual.GetRawText(),
        };
    }

    private static bool JsonObjectDeepEquals(JsonElement expected, JsonElement actual)
    {
        var expectedProperties = expected.EnumerateObject().ToList();
        var actualProperties = actual.EnumerateObject().ToList();

        if (expectedProperties.Count != actualProperties.Count)
        {
            return false;
        }

        foreach (var expectedProperty in expectedProperties.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (expectedProperty.Name.Length == 0)
            {
                continue;
            }

            var actualProperty = actualProperties.FirstOrDefault(p => p.Name == expectedProperty.Name);
            if (actualProperty.Name is null)
            {
                return false;
            }

            if (!JsonElementDeepEquals(expectedProperty.Value, actualProperty.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool JsonArrayDeepEquals(JsonElement expected, JsonElement actual)
    {
        var expectedItems = expected.EnumerateArray().ToList();
        var actualItems = actual.EnumerateArray().ToList();

        if (expectedItems.Count != actualItems.Count)
        {
            return false;
        }

        for (var i = 0; i < expectedItems.Count; i++)
        {
            if (!JsonElementDeepEquals(expectedItems[i], actualItems[i]))
            {
                return false;
            }
        }

        return true;
    }
}
