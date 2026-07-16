using EduCollab.Api.Config;
using EduCollab.Api.Security;
using Microsoft.Extensions.Options;

namespace EduCollab.Api.Tests;

public sealed class ContentDownloadTokenServiceTests
{
    [Fact]
    public void CreateAndValidate_Succeeds_ForMatchingUserWithinTtl()
    {
        var service = CreateService();
        var token = service.CreateAssetContentToken(userId: 7, workspaceId: 3, sceneId: 11, assetId: 42);

        Assert.True(service.TryValidateAssetContentToken(
            token,
            userId: 7,
            out var workspaceId,
            out var sceneId,
            out var assetId,
            out var flowId));
        Assert.Equal(3, workspaceId);
        Assert.Equal(11, sceneId);
        Assert.Equal(42, assetId);
        Assert.Null(flowId);
    }

    [Fact]
    public void CreateAndValidate_Succeeds_WithOptionalFlowId()
    {
        var service = CreateService();
        var token = service.CreateAssetContentToken(userId: 7, workspaceId: 3, sceneId: 11, assetId: 42, flowId: 9);

        Assert.True(service.TryValidateAssetContentToken(
            token,
            userId: 7,
            out var workspaceId,
            out var sceneId,
            out var assetId,
            out var flowId));
        Assert.Equal(3, workspaceId);
        Assert.Equal(11, sceneId);
        Assert.Equal(42, assetId);
        Assert.Equal(9, flowId);
    }

    [Fact]
    public void CreateAndValidate_SceneContent_Succeeds()
    {
        var service = CreateService();
        var token = service.CreateSceneContentToken(userId: 7, workspaceId: 3, flowId: 9, sceneId: 11);

        Assert.True(service.TryValidateSceneContentToken(
            token,
            userId: 7,
            out var workspaceId,
            out var flowId,
            out var sceneId));
        Assert.Equal(3, workspaceId);
        Assert.Equal(9, flowId);
        Assert.Equal(11, sceneId);
    }

    [Fact]
    public void Validate_Fails_WhenUserDoesNotMatch()
    {
        var service = CreateService();
        var token = service.CreateAssetContentToken(userId: 7, workspaceId: 3, sceneId: 11, assetId: 42);

        Assert.False(service.TryValidateAssetContentToken(token, userId: 8, out _, out _, out _, out _));
    }

    private static ContentDownloadTokenService CreateService() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "educollab-tests",
            Audience = "educollab-tests",
            SecretKey = "unit-test-secret-key-at-least-32-chars!",
            ExpirationMinutes = 60,
        }));
}
