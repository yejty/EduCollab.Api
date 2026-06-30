using System.Net;
using System.Net.Http.Headers;

namespace EduCollab.Api.Tests;

public sealed class AssetsControllerEndpointTests
{
    [Fact]
    public async Task CreateAsset_ReturnsBadRequest_WhenFileMissing()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 1);

        using var form = AssetTestHelpers.CreateAssetMultipartForm("Chair", groupId: 1, includeFile: false);
        var response = await client.PostAsync("/api/workspace/assets", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_content", body.Error);
        response.AssertProblemJsonResponse();
    }

    [Fact]
    public async Task CreateAsset_ReturnsBadRequest_WhenFileIsNotZip()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 1);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Chair"), "name");
        form.Add(new StringContent("1"), "groupId");

        var textFile = new ByteArrayContent("not a zip"u8.ToArray());
        textFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(textFile, "file", "notes.txt");

        var response = await client.PostAsync("/api/workspace/assets", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_content", body.Error);
    }
}
