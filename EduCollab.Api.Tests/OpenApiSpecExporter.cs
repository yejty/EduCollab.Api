namespace EduCollab.Api.Tests;

public static class OpenApiSpecExporter
{
    public static async Task ExportCommittedSpecAsync(CancellationToken cancellationToken = default)
    {
        var outputPath = OpenApiSpecTests.ResolveCommittedSpecPath();
        var directory = Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("Invalid OpenAPI output path.");

        Directory.CreateDirectory(directory);

        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }
}
