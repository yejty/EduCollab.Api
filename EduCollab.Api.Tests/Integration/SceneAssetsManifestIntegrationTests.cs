using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class SceneAssetsManifestIntegrationTests
{
    [Fact]
    public async Task SceneAssets_IncludeAssets_MintsToken_AndAllowsContextualDownload()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Scene Assets Manifest Workspace",
            "includeAssets + download token test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Member User",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var sharedGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Team",
        });
        sharedGroupResponse.EnsureSuccessStatusCode();
        var sharedGroup = await sharedGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var privateGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Owner Only Assets",
        });
        privateGroupResponse.EnsureSuccessStatusCode();
        var privateGroup = await privateGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var meResponse = await memberClient.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
        var member = await meResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var addMemberResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{sharedGroup.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) });
        addMemberResponse.EnsureSuccessStatusCode();

        var hiddenAsset = await ownerClient.PostAssetAsync("Hidden Asset", privateGroup.Id);
        var sceneJson = JsonNode.Parse(
            $$"""
              {
                "objects": [ { "assetId": {{hiddenAsset.Id}} } ]
              }
              """);

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Shared Scene",
            JsonContent = sceneJson,
            GroupIds = [sharedGroup.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var setSharesWithoutInclude = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/scene-groups?sceneId={scene.Id}",
            new SetSceneGroupsRequest
            {
                Groups = [new ResourceGroupShareRequest { GroupId = sharedGroup.Id, IncludeAssets = false }],
            });
        setSharesWithoutInclude.EnsureSuccessStatusCode();

        var manifestWithoutIncludeResponse = await memberClient.GetAsync(
            $"/api/workspace/scene-assets?sceneId={scene.Id}");
        manifestWithoutIncludeResponse.EnsureSuccessStatusCode();
        var manifestWithoutInclude = await manifestWithoutIncludeResponse.ReadAsJsonAsync<SceneAssetsResponse>();
        Assert.False(manifestWithoutInclude.IncludeAssets);
        Assert.False(string.IsNullOrWhiteSpace(manifestWithoutInclude.Message));
        var hiddenRow = Assert.Single(manifestWithoutInclude.Assets);
        Assert.False(hiddenRow.CanViewDirectly);
        Assert.False(hiddenRow.UsableInScene);
        Assert.Null(hiddenRow.DownloadToken);

        var setSharesWithInclude = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/scene-groups?sceneId={scene.Id}",
            new SetSceneGroupsRequest
            {
                Groups = [new ResourceGroupShareRequest { GroupId = sharedGroup.Id, IncludeAssets = true }],
            });
        setSharesWithInclude.EnsureSuccessStatusCode();

        var shareListResponse = await ownerClient.GetAsync($"/api/workspace/scene-groups?sceneId={scene.Id}");
        shareListResponse.EnsureSuccessStatusCode();
        var shareList = await shareListResponse.ReadAsJsonAsync<ResourceGroupsResponse>();
        Assert.Contains(shareList.Groups, g => g.GroupId == sharedGroup.Id && g.IncludeAssets);

        var manifestResponse = await memberClient.GetAsync($"/api/workspace/scene-assets?sceneId={scene.Id}");
        manifestResponse.EnsureSuccessStatusCode();
        var manifest = await manifestResponse.ReadAsJsonAsync<SceneAssetsResponse>();
        Assert.True(manifest.IncludeAssets);
        Assert.Null(manifest.Message);
        var assetRow = Assert.Single(manifest.Assets);
        Assert.False(assetRow.CanViewDirectly);
        Assert.True(assetRow.UsableInScene);
        Assert.False(string.IsNullOrWhiteSpace(assetRow.DownloadToken));
        Assert.False(string.IsNullOrWhiteSpace(assetRow.CacheKey));

        var directContentResponse = await memberClient.GetAsync($"/api/workspace/assets/{hiddenAsset.Id}/content");
        Assert.Equal(HttpStatusCode.Forbidden, directContentResponse.StatusCode);

        var tokenContentResponse = await memberClient.GetAsync(
            $"/api/workspace/scene-assets/content?token={Uri.EscapeDataString(assetRow.DownloadToken!)}");
        tokenContentResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", tokenContentResponse.Content.Headers.ContentType?.MediaType);

        var contentWithoutToken = await memberClient.GetAsync("/api/workspace/scene-assets/content");
        Assert.Equal(HttpStatusCode.BadRequest, contentWithoutToken.StatusCode);
    }

    [Fact]
    public async Task SceneAssets_PeerVisibleAsset_SkipsDownloadToken()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Peer Asset Workspace",
            "canViewDirectly skips tokens");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes", "loadAssets"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Member User",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var sharedGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Team",
        });
        sharedGroupResponse.EnsureSuccessStatusCode();
        var sharedGroup = await sharedGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var meResponse = await memberClient.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
        var member = await meResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var addMemberResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{sharedGroup.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) });
        addMemberResponse.EnsureSuccessStatusCode();

        var sharedAsset = await ownerClient.PostAssetAsync("Shared Asset", sharedGroup.Id);
        var sceneJson = JsonNode.Parse(
            $$"""
              {
                "objects": [ { "assetId": {{sharedAsset.Id}} } ]
              }
              """);

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Shared Scene",
            JsonContent = sceneJson,
            GroupIds = [sharedGroup.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var manifestResponse = await memberClient.GetAsync($"/api/workspace/scene-assets?sceneId={scene.Id}");
        manifestResponse.EnsureSuccessStatusCode();
        var manifest = await manifestResponse.ReadAsJsonAsync<SceneAssetsResponse>();
        var assetRow = Assert.Single(manifest.Assets);
        Assert.True(assetRow.CanViewDirectly);
        Assert.True(assetRow.UsableInScene);
        Assert.Null(assetRow.DownloadToken);

        var peerContentResponse = await memberClient.GetAsync($"/api/workspace/assets/{sharedAsset.Id}/content");
        peerContentResponse.EnsureSuccessStatusCode();
    }
}
