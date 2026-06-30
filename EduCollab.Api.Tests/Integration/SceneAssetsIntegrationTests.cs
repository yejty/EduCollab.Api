using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Scenes;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class SceneAssetsIntegrationTests
{
    [Fact]
    public async Task SceneAssets_ResolvesJsonReferencesAndAttachments_WithSceneContextAccessFlags()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Scene Assets Workspace",
            "Scene asset integration test");

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Viewer",
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Member",
                LastName = "User",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var sharedGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Team",
            Description = "Scene and assets visible to the member",
        });
        sharedGroupResponse.EnsureSuccessStatusCode();
        var sharedGroup = await sharedGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var privateGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Owner Only",
            Description = "Assets hidden from the viewer member",
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

        var sharedAsset = await ownerClient.PostAssetAsync("Shared Asset", sharedGroup.Id);

        var hiddenAsset = await ownerClient.PostAssetAsync("Hidden Asset", privateGroup.Id);

        var attachAsset = await ownerClient.PostAssetAsync("Attach Target", sharedGroup.Id);

        var sceneJson = JsonNode.Parse(
            $$"""
              {
                "objects": [
                  { "assetId": {{sharedAsset.Id}} },
                  { "assetId": {{hiddenAsset.Id}} }
                ]
              }
              """);

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Lesson Scene",
            Description = "References shared and hidden assets",
            JsonContent = sceneJson,
            GroupId = sharedGroup.Id,
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var memberListResponse = await memberClient.GetAsync($"/api/workspace/scene-assets?sceneId={scene.Id}");
        memberListResponse.EnsureSuccessStatusCode();
        var memberList = await memberListResponse.ReadAsJsonAsync<SceneAssetsResponse>();

        Assert.Equal(2, memberList.Assets.Count);
        Assert.All(memberList.Assets, asset => Assert.True(asset.UsableInScene));

        var sharedFromJson = Assert.Single(memberList.Assets, asset => asset.AssetId == sharedAsset.Id);
        Assert.Equal("SceneJsonReference", sharedFromJson.ResolvedFrom);
        Assert.True(sharedFromJson.CanViewDirectly);

        var hiddenFromJson = Assert.Single(memberList.Assets, asset => asset.AssetId == hiddenAsset.Id);
        Assert.Equal("SceneJsonReference", hiddenFromJson.ResolvedFrom);
        Assert.False(hiddenFromJson.CanViewDirectly);

        var memberSharedAssetResponse = await memberClient.GetAsync($"/api/workspace/assets/{sharedAsset.Id}");
        memberSharedAssetResponse.EnsureSuccessStatusCode();

        var memberHiddenAssetResponse = await memberClient.GetAsync($"/api/workspace/assets/{hiddenAsset.Id}");
        Assert.Equal(HttpStatusCode.NotFound, memberHiddenAssetResponse.StatusCode);

        var memberHiddenContentResponse = await memberClient.GetAsync(
            $"/api/workspace/assets/{hiddenAsset.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, memberHiddenContentResponse.StatusCode);

        var memberSceneContextContentResponse = await memberClient.GetAsync(
            $"/api/workspace/scene-assets/content?sceneId={scene.Id}&assetId={hiddenAsset.Id}");
        memberSceneContextContentResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", memberSceneContextContentResponse.Content.Headers.ContentType?.MediaType);

        var ownerAttachResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scene-assets", new AttachSceneAssetRequest
        {
            SceneId = scene.Id,
            AssetId = attachAsset.Id,
        });
        Assert.Equal(HttpStatusCode.Created, ownerAttachResponse.StatusCode);
        var attached = await ownerAttachResponse.ReadAsJsonAsync<SceneAssetResponse>();
        Assert.Equal(attachAsset.Id, attached.AssetId);
        Assert.Equal("SceneAttachment", attached.ResolvedFrom);
        Assert.True(attached.UsableInScene);

        var afterAttachResponse = await memberClient.GetAsync($"/api/workspace/scene-assets?sceneId={scene.Id}");
        afterAttachResponse.EnsureSuccessStatusCode();
        var afterAttach = await afterAttachResponse.ReadAsJsonAsync<SceneAssetsResponse>();
        Assert.Equal(3, afterAttach.Assets.Count);
        Assert.Contains(afterAttach.Assets, asset => asset.AssetId == attachAsset.Id && asset.ResolvedFrom == "SceneAttachment");

        var memberAttachResponse = await memberClient.PostAsJsonAsync("/api/workspace/scene-assets", new AttachSceneAssetRequest
        {
            SceneId = scene.Id,
            AssetId = sharedAsset.Id,
        });
        Assert.Equal(HttpStatusCode.Forbidden, memberAttachResponse.StatusCode);

        var ownerDetachResponse = await ownerClient.DeleteAsync(
            $"/api/workspace/scene-assets?sceneId={scene.Id}&assetId={attachAsset.Id}");
        Assert.Equal(HttpStatusCode.NoContent, ownerDetachResponse.StatusCode);

        var afterDetachResponse = await memberClient.GetAsync($"/api/workspace/scene-assets?sceneId={scene.Id}");
        afterDetachResponse.EnsureSuccessStatusCode();
        var afterDetach = await afterDetachResponse.ReadAsJsonAsync<SceneAssetsResponse>();
        Assert.Equal(2, afterDetach.Assets.Count);
        Assert.DoesNotContain(afterDetach.Assets, asset => asset.AssetId == attachAsset.Id);
    }

    [Fact]
    public async Task SceneAssets_ReturnsNotFound_WhenSceneIsNotVisibleToCaller()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Private Scene Workspace",
            "Scene visibility integration test");

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Viewer",
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Member",
                LastName = "User",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var privateGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Owner Only",
        });
        privateGroupResponse.EnsureSuccessStatusCode();
        var privateGroup = await privateGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Private Scene",
            JsonContent = JsonNode.Parse("{}"),
            GroupId = privateGroup.Id,
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var memberListResponse = await memberClient.GetAsync($"/api/workspace/scene-assets?sceneId={scene.Id}");
        Assert.Equal(HttpStatusCode.NotFound, memberListResponse.StatusCode);

        var memberAttachResponse = await memberClient.PostAsJsonAsync("/api/workspace/scene-assets", new AttachSceneAssetRequest
        {
            SceneId = scene.Id,
            AssetId = 1,
        });
        Assert.Equal(HttpStatusCode.Forbidden, memberAttachResponse.StatusCode);
    }

    [Fact]
    public async Task CreateScene_ReturnsBadRequest_WhenJsonReferencesMissingAsset()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Scene Validation Workspace",
            "Scene asset reference validation test");

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Team",
        });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Broken Scene",
            JsonContent = JsonNode.Parse("""{ "objects": [ { "assetId": 999999 } ] }"""),
            GroupId = group.Id,
        });

        Assert.Equal(HttpStatusCode.BadRequest, createSceneResponse.StatusCode);
        var problem = await createSceneResponse.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_asset_reference", problem.Error);
    }

    [Fact]
    public async Task CreateSceneFromForm_AcceptsJsonFile()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Scene Multipart Workspace",
            "Scene multipart create test");

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Team",
        });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Imported Scene"), "name");
        form.Add(new StringContent(group.Id.ToString()), "groupId");
        form.Add(new StringContent("""{ "objects": [] }""", Encoding.UTF8, "application/json"), "jsonFile", "scene.json");

        var createSceneResponse = await ownerClient.PostAsync("/api/workspace/scenes", form);
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();
        Assert.Equal("Imported Scene", scene.Name);
    }
}
