using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduCollab.Contracts.Responses.Assets;

namespace EduCollab.Api.Tests;

internal static class AssetTestHelpers
{
    private static readonly byte[] MinimalZipBytes =
    [
        0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    ];

    public static ByteArrayContent CreateMinimalZipFileContent(string fileName = "asset.zip")
    {
        var content = new ByteArrayContent(MinimalZipBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileName = $"\"{fileName}\"",
        };
        return content;
    }

    public static MultipartFormDataContent CreateAssetMultipartForm(
        string name,
        int groupId,
        string? description = null,
        bool includeFile = true,
        string zipFileName = "asset.zip")
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "name");
        form.Add(new StringContent(groupId.ToString()), "groupId");

        if (!string.IsNullOrWhiteSpace(description))
            form.Add(new StringContent(description), "description");

        if (includeFile)
            form.Add(CreateMinimalZipFileContent(zipFileName), "file", zipFileName);

        return form;
    }

    public static async Task<AssetResponse> PostAssetAsync(
        this HttpClient client,
        string name,
        int groupId,
        string? description = null)
    {
        using var form = CreateAssetMultipartForm(name, groupId, description);
        var response = await client.PostAsync("/api/workspace/assets", form);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsJsonAsync<AssetResponse>();
    }
}
