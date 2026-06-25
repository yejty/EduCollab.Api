using EduCollab.Application.Services.Assets;

namespace EduCollab.Api.Tests;

public sealed class AssetContentFormatsTests
{
    [Theory]
    [InlineData("application/zip", "asset.zip", true)]
    [InlineData("application/x-zip-compressed", "asset.zip", true)]
    [InlineData("application/zip", null, true)]
    [InlineData("application/json", "asset.zip", true)]
    [InlineData("application/json", "asset.json", false)]
    [InlineData("text/plain", "readme.txt", false)]
    public void IsZipContent_ReturnsExpectedResult(string contentType, string? fileName, bool expected)
    {
        Assert.Equal(expected, AssetContentFormats.IsZipContent(contentType, fileName));
    }
}
