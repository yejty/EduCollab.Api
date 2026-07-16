using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Responses.Scenes;
using EduCollab.Contracts.Responses.Flows;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ParameterAuthorizationIntegrationTests
{
    [Fact]
    public async Task CustomMember_WithAddAssetsAndLoadScenes_CanCreateAssets_ButCannotInvite()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"custom-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Custom Preset Workspace",
            "Preset authorization integration test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["addAssets", "loadScenes", "loadFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Custom Member",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared",
        });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var meResponse = await memberClient.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
        var member = await meResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var addToGroupResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) });
        addToGroupResponse.EnsureSuccessStatusCode();

        var createdAsset = await memberClient.PostAssetAsync("Custom Asset", group.Id);
        Assert.True(createdAsset.Id > 0);

        var inviteAttempt = await memberClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = $"blocked-{Guid.NewGuid():N}@example.com",
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes", "loadFlows"],
        });
        Assert.Equal(HttpStatusCode.Forbidden, inviteAttempt.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotListAssets_EvenWhenGroupMember()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Viewer Asset Gate", "Preset test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Viewer),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(viewerEmail);
        var acceptResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "View Only",
                Email = viewerEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var viewerTokens = await viewerClient.LoginAsync(viewerEmail, password);
        viewerClient.SetBearerToken(viewerTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Team" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var meResponse = await viewerClient.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
        var viewer = await meResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)viewer.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        await ownerClient.PostAssetAsync("Team Asset", group.Id);

        var listAssetsResponse = await viewerClient.GetAsync("/api/workspace/assets");
        Assert.Equal(HttpStatusCode.Forbidden, listAssetsResponse.StatusCode);

        var groupAssetsResponse = await viewerClient.GetAsync($"/api/workspace/groups/{group.Id}/assets");
        Assert.Equal(HttpStatusCode.Forbidden, groupAssetsResponse.StatusCode);
    }

    [Fact]
    public async Task MemberWithoutSeeUsersTab_CannotListOrGetUsersById()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"creator-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "See Users Tab Gate",
            "Member lookup without users tab preset");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Creator),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Creator Member",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var listMembersResponse = await memberClient.GetAsync("/api/workspace/users");
        Assert.Equal(HttpStatusCode.Forbidden, listMembersResponse.StatusCode);

        var getMemberResponse = await memberClient.GetAsync($"/api/workspace/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getMemberResponse.StatusCode);

        var getUserResponse = await memberClient.GetAsync($"/api/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getUserResponse.StatusCode);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Team" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var memberMeResponse = await memberClient.GetAsync("/api/users/me");
        memberMeResponse.EnsureSuccessStatusCode();
        var member = await memberMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var listGroupMembersResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/users");
        Assert.Equal(HttpStatusCode.Forbidden, listGroupMembersResponse.StatusCode);

        var getGroupMemberResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getGroupMemberResponse.StatusCode);
    }

    [Fact]
    public async Task ViewerWithLoadScenesOnly_CannotGetUserById()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Viewer User Gate", "Preset test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Viewer),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(viewerEmail);
        var acceptResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "View Only",
                Email = viewerEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var viewerTokens = await viewerClient.LoginAsync(viewerEmail, password);
        viewerClient.SetBearerToken(viewerTokens.AccessToken);

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var getMemberResponse = await viewerClient.GetAsync($"/api/workspace/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getMemberResponse.StatusCode);

        var getUserResponse = await viewerClient.GetAsync($"/api/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getUserResponse.StatusCode);

        var meResponse = await viewerClient.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ViewerWithLoadScenesOnly_CannotListOrGetGroupMembersEvenWhenInGroup()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Viewer Group Members Gate", "Preset test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Viewer),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(viewerEmail);
        var acceptResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "View Only",
                Email = viewerEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var viewerTokens = await viewerClient.LoginAsync(viewerEmail, password);
        viewerClient.SetBearerToken(viewerTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Team" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var viewerMeResponse = await viewerClient.GetAsync("/api/users/me");
        viewerMeResponse.EnsureSuccessStatusCode();
        var viewer = await viewerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)viewer.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var listMembersResponse = await viewerClient.GetAsync($"/api/workspace/groups/{group.Id}/users");
        Assert.Equal(HttpStatusCode.Forbidden, listMembersResponse.StatusCode);

        var getOwnerMemberResponse = await viewerClient.GetAsync($"/api/workspace/groups/{group.Id}/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getOwnerMemberResponse.StatusCode);

        var getSelfMemberResponse = await viewerClient.GetAsync($"/api/workspace/groups/{group.Id}/users/{viewer.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getSelfMemberResponse.StatusCode);
    }

    [Fact]
    public async Task ViewerWithLoadScenesOnly_CannotGetSceneOrFlowGroupShares()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Viewer Group Shares Gate", "Preset test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Viewer),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(viewerEmail);
        var acceptResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "View Only",
                Email = viewerEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var viewerTokens = await viewerClient.LoginAsync(viewerEmail, password);
        viewerClient.SetBearerToken(viewerTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Shared" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var viewerMeResponse = await viewerClient.GetAsync("/api/users/me");
        viewerMeResponse.EnsureSuccessStatusCode();
        var viewer = await viewerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)viewer.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var createSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Shared Scene",
            JsonContent = "{}",
            GroupIds = [group.Id],
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var createFlowResponse = await ownerClient.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Shared Flow",
            GroupIds = [group.Id],
            SceneIds = [scene.Id],
        });
        createFlowResponse.EnsureSuccessStatusCode();
        var flow = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();

        var sceneGroupsResponse = await viewerClient.GetAsync($"/api/workspace/scene-groups?sceneId={scene.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, sceneGroupsResponse.StatusCode);

        var flowGroupsResponse = await viewerClient.GetAsync($"/api/workspace/flow-groups?flowId={flow.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, flowGroupsResponse.StatusCode);

        var sceneResponse = await viewerClient.GetAsync($"/api/workspace/scenes/{scene.Id}");
        sceneResponse.EnsureSuccessStatusCode();
        var visibleScene = await sceneResponse.ReadAsJsonAsync<SceneResponse>();
        Assert.Empty(visibleScene.GroupIds);

        var flowResponse = await viewerClient.GetAsync($"/api/workspace/flows/{flow.Id}");
        flowResponse.EnsureSuccessStatusCode();
        var visibleFlow = await flowResponse.ReadAsJsonAsync<FlowResponse>();
        Assert.Empty(visibleFlow.GroupIds);
    }

    [Fact]
    public async Task MemberWithLoadAssetsOnly_CannotGetAssetGroupShares()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"loader-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Asset Group Shares Gate", "Preset test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadAssets", "loadScenes", "loadFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Load Only",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Shared" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var memberMeResponse = await memberClient.GetAsync("/api/users/me");
        memberMeResponse.EnsureSuccessStatusCode();
        var member = await memberMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var asset = await ownerClient.PostAssetAsync("Shared Asset", group.Id);

        var assetGroupsResponse = await memberClient.GetAsync($"/api/workspace/asset-groups?assetId={asset.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, assetGroupsResponse.StatusCode);

        var assetResponse = await memberClient.GetAsync($"/api/workspace/assets/{asset.Id}");
        assetResponse.EnsureSuccessStatusCode();
        var visibleAsset = await assetResponse.ReadAsJsonAsync<AssetResponse>();
        Assert.Empty(visibleAsset.GroupIds);
    }
}
