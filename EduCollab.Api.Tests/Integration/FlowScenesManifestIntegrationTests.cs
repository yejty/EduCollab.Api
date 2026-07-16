using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Flows;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class FlowScenesManifestIntegrationTests
{
    [Fact]
    public async Task FlowScenes_IncludeAssets_MintsToken_AndAllowsContextualSceneAndNestedAssetDownload()
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
            "Flow Scenes Manifest Workspace",
            "includeAssets + flow-scenes + nested scene-assets");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadFlows"],
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
            Name = "Owner Only",
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
            Name = "Private Scene",
            JsonContent = sceneJson,
            GroupIds = [privateGroup.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var createFlowResponse = await ownerClient.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Shared Flow",
            GroupIds = [sharedGroup.Id],
            SceneIds = [scene.Id],
        });
        createFlowResponse.EnsureSuccessStatusCode();
        var flow = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();
        Assert.Contains(scene.Id, flow.SceneIds);

        var setSharesWithoutInclude = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/flow-groups?flowId={flow.Id}",
            new SetFlowGroupsRequest
            {
                Groups = [new ResourceGroupShareRequest { GroupId = sharedGroup.Id, IncludeAssets = false }],
            });
        setSharesWithoutInclude.EnsureSuccessStatusCode();

        var scenesWithoutIncludeResponse = await memberClient.GetAsync(
            $"/api/workspace/flow-scenes?flowId={flow.Id}");
        scenesWithoutIncludeResponse.EnsureSuccessStatusCode();
        var scenesWithoutInclude = await scenesWithoutIncludeResponse.ReadAsJsonAsync<FlowScenesResponse>();
        Assert.False(scenesWithoutInclude.IncludeAssets);
        Assert.False(string.IsNullOrWhiteSpace(scenesWithoutInclude.Message));
        var hiddenSceneRow = Assert.Single(scenesWithoutInclude.Scenes);
        Assert.False(hiddenSceneRow.CanViewDirectly);
        Assert.False(hiddenSceneRow.UsableInFlow);
        Assert.Null(hiddenSceneRow.DownloadToken);

        var setSharesWithInclude = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/flow-groups?flowId={flow.Id}",
            new SetFlowGroupsRequest
            {
                Groups = [new ResourceGroupShareRequest { GroupId = sharedGroup.Id, IncludeAssets = true }],
            });
        setSharesWithInclude.EnsureSuccessStatusCode();

        var shareListResponse = await ownerClient.GetAsync($"/api/workspace/flow-groups?flowId={flow.Id}");
        shareListResponse.EnsureSuccessStatusCode();
        var shareList = await shareListResponse.ReadAsJsonAsync<ResourceGroupsResponse>();
        Assert.Contains(shareList.Groups, g => g.GroupId == sharedGroup.Id && g.IncludeAssets);

        var scenesResponse = await memberClient.GetAsync($"/api/workspace/flow-scenes?flowId={flow.Id}");
        scenesResponse.EnsureSuccessStatusCode();
        var scenesManifest = await scenesResponse.ReadAsJsonAsync<FlowScenesResponse>();
        Assert.True(scenesManifest.IncludeAssets);
        Assert.Null(scenesManifest.Message);
        var sceneRow = Assert.Single(scenesManifest.Scenes);
        Assert.False(sceneRow.CanViewDirectly);
        Assert.True(sceneRow.UsableInFlow);
        Assert.False(string.IsNullOrWhiteSpace(sceneRow.DownloadToken));
        Assert.False(string.IsNullOrWhiteSpace(sceneRow.CacheKey));

        var directSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{scene.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, directSceneResponse.StatusCode);

        var tokenSceneResponse = await memberClient.GetAsync(
            $"/api/workspace/flow-scenes/content?token={Uri.EscapeDataString(sceneRow.DownloadToken!)}");
        tokenSceneResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/json", tokenSceneResponse.Content.Headers.ContentType?.MediaType);

        var nestedAssetsWithoutFlowId = await memberClient.GetAsync(
            $"/api/workspace/scene-assets?sceneId={scene.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, nestedAssetsWithoutFlowId.StatusCode);

        var nestedAssetsResponse = await memberClient.GetAsync(
            $"/api/workspace/scene-assets?sceneId={scene.Id}&flowId={flow.Id}");
        nestedAssetsResponse.EnsureSuccessStatusCode();
        var nestedAssets = await nestedAssetsResponse.ReadAsJsonAsync<SceneAssetsResponse>();
        Assert.True(nestedAssets.IncludeAssets);
        var assetRow = Assert.Single(nestedAssets.Assets);
        Assert.False(assetRow.CanViewDirectly);
        Assert.True(assetRow.UsableInScene);
        Assert.False(string.IsNullOrWhiteSpace(assetRow.DownloadToken));

        var directAssetResponse = await memberClient.GetAsync($"/api/workspace/assets/{hiddenAsset.Id}/content");
        Assert.Equal(HttpStatusCode.Forbidden, directAssetResponse.StatusCode);

        var tokenAssetResponse = await memberClient.GetAsync(
            $"/api/workspace/scene-assets/content?token={Uri.EscapeDataString(assetRow.DownloadToken!)}");
        tokenAssetResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", tokenAssetResponse.Content.Headers.ContentType?.MediaType);

        var contentWithoutToken = await memberClient.GetAsync("/api/workspace/flow-scenes/content");
        Assert.Equal(HttpStatusCode.BadRequest, contentWithoutToken.StatusCode);
    }

    [Fact]
    public async Task FlowScenes_PeerVisibleScene_SkipsDownloadToken()
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
            "Peer Scene Workspace",
            "canViewDirectly skips tokens");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadFlows", "loadScenes"],
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

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Shared Scene",
            JsonContent = "{}",
            GroupIds = [sharedGroup.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var createFlowResponse = await ownerClient.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Shared Flow",
            GroupIds = [sharedGroup.Id],
            SceneIds = [scene.Id],
        });
        createFlowResponse.EnsureSuccessStatusCode();
        var flow = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();

        var scenesResponse = await memberClient.GetAsync($"/api/workspace/flow-scenes?flowId={flow.Id}");
        scenesResponse.EnsureSuccessStatusCode();
        var scenesManifest = await scenesResponse.ReadAsJsonAsync<FlowScenesResponse>();
        var sceneRow = Assert.Single(scenesManifest.Scenes);
        Assert.True(sceneRow.CanViewDirectly);
        Assert.True(sceneRow.UsableInFlow);
        Assert.Null(sceneRow.DownloadToken);

        var peerSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{scene.Id}");
        peerSceneResponse.EnsureSuccessStatusCode();
    }
}
