using System.Text.Json;
using System.Text.Json.Nodes;

namespace Drive.Api.Tests;

public sealed class OpenApiSpecTests
{
    [Fact]
    public async Task LiveSwaggerDocument_isAvailable()
    {
        await using var factory = new DriveApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Drive API (parked)", document?["info"]?["title"]?.GetValue<string>());
        Assert.NotNull(document?["paths"]);
        Assert.True(document!["paths"]!.AsObject().Count > 0);
        Assert.NotNull(document["paths"]?["/api/workspace/assets"]);
        Assert.Null(document["paths"]?["/api/users/login"]);
    }

    [Fact]
    public async Task CommittedOpenApiSpec_matchesLiveSwaggerDocument()
    {
        var specPath = ResolveCommittedSpecPath();
        Assert.True(File.Exists(specPath), $"Committed OpenAPI spec not found at {specPath}. Run scripts/export-openapi.ps1.");

        var expectedJson = await File.ReadAllTextAsync(specPath);

        await using var factory = new DriveApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var actualJson = await response.Content.ReadAsStringAsync();

        Assert.True(
            JsonSemanticEquals(expectedJson, actualJson),
            "Committed docs/drive-target.openapi.json is out of date. Run scripts/export-openapi.ps1.");
    }

    [Fact]
    public async Task ExportCommittedOpenApiSpec_WhenExportFlagIsSet()
    {
        if (Environment.GetEnvironmentVariable("EXPORT_OPENAPI") != "1")
        {
            return;
        }

        var outputPath = ResolveCommittedSpecPath();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        await using var factory = new DriveApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        await File.WriteAllTextAsync(outputPath, await response.Content.ReadAsStringAsync());
    }

    public static string ResolveCommittedSpecPath()
    {
        var root = ResolveRepoRoot();
        return Path.Combine(root, "docs", "drive-target.openapi.json");
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
            ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    private static bool JsonSemanticEquals(string expectedJson, string actualJson)
    {
        using var expected = JsonDocument.Parse(expectedJson);
        using var actual = JsonDocument.Parse(actualJson);
        return JsonNode.DeepEquals(
            JsonNode.Parse(expected.RootElement.GetRawText()),
            JsonNode.Parse(actual.RootElement.GetRawText()));
    }
}
