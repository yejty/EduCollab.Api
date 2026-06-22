using System.Text.Json;

namespace EduCollab.Api.Tests;

public sealed class PostmanCollectionTests
{
    [Fact]
    public async Task ExportCommittedPostmanCollection_WhenExportFlagIsSet()
    {
        if (Environment.GetEnvironmentVariable("EXPORT_POSTMAN") != "1")
        {
            return;
        }

        var openApiPath = OpenApiSpecTests.ResolveCommittedSpecPath();
        var outputPath = PostmanCollectionExporter.ResolveCommittedCollectionPath();
        await PostmanCollectionExporter.ExportFromOpenApiAsync(openApiPath, outputPath);
    }

    [Fact]
    public async Task CommittedPostmanCollection_matchesOpenApiExport()
    {
        var openApiPath = OpenApiSpecTests.ResolveCommittedSpecPath();
        var collectionPath = PostmanCollectionExporter.ResolveCommittedCollectionPath();

        Assert.True(File.Exists(openApiPath), $"OpenAPI spec not found at {openApiPath}.");
        Assert.True(File.Exists(collectionPath), $"Postman collection not found at {collectionPath}. Run scripts/export-postman.ps1.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"educollab-postman-{Guid.NewGuid():N}.json");
        try
        {
            await PostmanCollectionExporter.ExportFromOpenApiAsync(openApiPath, tempPath);
            var expectedJson = await File.ReadAllTextAsync(collectionPath);
            var actualJson = await File.ReadAllTextAsync(tempPath);

            Assert.True(
                OpenApiSpecTests.JsonSemanticEqualsPublic(expectedJson, actualJson),
                "Committed EduCollab.Api.postman_collection.json is out of date. Run scripts/export-postman.ps1.");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
