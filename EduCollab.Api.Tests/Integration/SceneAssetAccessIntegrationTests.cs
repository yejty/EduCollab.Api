using System.Net;
using System.Net.Http.Json;
using System.Text;
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
public sealed class SceneAssetAccessIntegrationTests
{
    [Fact]
    public async Task GetScene_ReturnsJson_WhenSceneIsVisible_EvenIfReferencedAssetIsInaccessible()
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
            "Scene Access Workspace",
            "Scene asset access integration test");

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Preset = "viewer",
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

        var sharedGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Team",
            Description = "Scene visible to the member",
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
            GroupIds = [sharedGroup.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var memberSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{scene.Id}");
        memberSceneResponse.EnsureSuccessStatusCode();
        var memberScene = await memberSceneResponse.ReadAsJsonAsync<SceneResponse>();
        Assert.NotNull(memberScene.JsonContent);

        var memberSharedAssetResponse = await memberClient.GetAsync($"/api/workspace/assets/{sharedAsset.Id}");
        memberSharedAssetResponse.EnsureSuccessStatusCode();

        var memberHiddenAssetResponse = await memberClient.GetAsync($"/api/workspace/assets/{hiddenAsset.Id}");
        Assert.Equal(HttpStatusCode.NotFound, memberHiddenAssetResponse.StatusCode);

        var memberHiddenContentResponse = await memberClient.GetAsync(
            $"/api/workspace/assets/{hiddenAsset.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, memberHiddenContentResponse.StatusCode);
    }

    [Fact]
    public async Task GetScene_ReturnsNotFound_WhenSceneIsNotVisibleToCaller()
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
            Preset = "viewer",
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
            GroupIds = [privateGroup.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var memberSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{scene.Id}");
        Assert.Equal(HttpStatusCode.NotFound, memberSceneResponse.StatusCode);
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
            GroupIds = [group.Id],
        });

        Assert.Equal(HttpStatusCode.BadRequest, createSceneResponse.StatusCode);
        var problem = await createSceneResponse.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_asset_reference", problem.Error);
    }

    [Fact]
    public async Task GetFlow_ReturnsSceneIds_WhenFlowIsVisible_EvenIfAttachedSceneIsInaccessible()
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
            "Flow Access Workspace",
            "Flow scene access integration test");

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Preset = "viewer",
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

        var sharedSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Shared Scene",
            JsonContent = JsonNode.Parse("{}"),
            GroupIds = [sharedGroup.Id],
        });
        sharedSceneResponse.EnsureSuccessStatusCode();
        var sharedScene = await sharedSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var privateSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Private Scene",
            JsonContent = JsonNode.Parse("{}"),
            GroupIds = [privateGroup.Id],
        });
        privateSceneResponse.EnsureSuccessStatusCode();
        var privateScene = await privateSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var createFlowResponse = await ownerClient.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Lesson Flow",
            GroupIds = [sharedGroup.Id],
            SceneIds = [sharedScene.Id, privateScene.Id],
        });
        createFlowResponse.EnsureSuccessStatusCode();
        var flow = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();

        var memberFlowResponse = await memberClient.GetAsync($"/api/workspace/flows/{flow.Id}");
        memberFlowResponse.EnsureSuccessStatusCode();
        var memberFlow = await memberFlowResponse.ReadAsJsonAsync<FlowResponse>();
        Assert.Contains(sharedScene.Id, memberFlow.SceneIds);
        Assert.Contains(privateScene.Id, memberFlow.SceneIds);

        var memberPrivateSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{privateScene.Id}");
        Assert.Equal(HttpStatusCode.NotFound, memberPrivateSceneResponse.StatusCode);

        var memberSharedSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{sharedScene.Id}");
        memberSharedSceneResponse.EnsureSuccessStatusCode();
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
        form.Add(new StringContent(group.Id.ToString()), "groupIds");
        form.Add(new StringContent("""{ "objects": [] }""", Encoding.UTF8, "application/json"), "jsonFile", "scene.json");

        var createSceneResponse = await ownerClient.PostAsync("/api/workspace/scenes", form);
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();
        Assert.Equal("Imported Scene", scene.Name);
    }
}
