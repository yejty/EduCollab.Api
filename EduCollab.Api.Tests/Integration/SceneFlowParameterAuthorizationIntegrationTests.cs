using System.Net;
using System.Net.Http.Json;
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
public sealed class SceneFlowParameterAuthorizationIntegrationTests
{
    [Fact]
    public async Task LoadScenesAndFlowsOnly_CanReadScenesAndFlows_ButCannotMutate()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"loader-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Scene Flow Load Gate", "Scene and flow parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes", "loadFlows"],
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

        var listScenesResponse = await memberClient.GetAsync("/api/workspace/scenes");
        listScenesResponse.EnsureSuccessStatusCode();

        var getSceneResponse = await memberClient.GetAsync($"/api/workspace/scenes/{scene.Id}");
        getSceneResponse.EnsureSuccessStatusCode();

        var listFlowsResponse = await memberClient.GetAsync("/api/workspace/flows");
        listFlowsResponse.EnsureSuccessStatusCode();

        var getFlowResponse = await memberClient.GetAsync($"/api/workspace/flows/{flow.Id}");
        getFlowResponse.EnsureSuccessStatusCode();

        var groupScenesResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/scenes");
        groupScenesResponse.EnsureSuccessStatusCode();

        var groupFlowsResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/flows");
        groupFlowsResponse.EnsureSuccessStatusCode();

        var createSceneAttempt = await memberClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Blocked Scene",
            JsonContent = "{}",
        });
        Assert.Equal(HttpStatusCode.Forbidden, createSceneAttempt.StatusCode);

        var createFlowAttempt = await memberClient.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Blocked Flow",
        });
        Assert.Equal(HttpStatusCode.Forbidden, createFlowAttempt.StatusCode);

        var sceneGroupsResponse = await memberClient.GetAsync($"/api/workspace/scene-groups?sceneId={scene.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, sceneGroupsResponse.StatusCode);

        var flowGroupsResponse = await memberClient.GetAsync($"/api/workspace/flow-groups?flowId={flow.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, flowGroupsResponse.StatusCode);
    }

    [Fact]
    public async Task AddScenesAndFlowsOnly_CanReadAndMutateOwnScenesAndFlows()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"creator-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Scene Flow Add Gate", "Scene and flow parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["addScenes", "addFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Add Only",
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

        var ownerSceneResponse = await ownerClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Owner Scene",
            JsonContent = "{}",
            GroupIds = [group.Id],
        });
        ownerSceneResponse.EnsureSuccessStatusCode();
        var ownerScene = await ownerSceneResponse.ReadAsJsonAsync<SceneResponse>();

        (await memberClient.GetAsync("/api/workspace/scenes")).EnsureSuccessStatusCode();
        (await memberClient.GetAsync($"/api/workspace/scenes/{ownerScene.Id}")).EnsureSuccessStatusCode();
        (await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/scenes")).EnsureSuccessStatusCode();

        var createSceneResponse = await memberClient.PostAsJsonAsync("/api/workspace/scenes", new CreateSceneRequest
        {
            Name = "Member Scene",
            JsonContent = "{}",
        });
        createSceneResponse.EnsureSuccessStatusCode();
        var scene = await createSceneResponse.ReadAsJsonAsync<SceneResponse>();

        var updateSceneResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/scenes/{scene.Id}",
            new UpdateSceneRequest { Name = "Updated Scene", JsonContent = "{}" });
        updateSceneResponse.EnsureSuccessStatusCode();

        var createFlowResponse = await memberClient.PostAsJsonAsync("/api/workspace/flows", new CreateFlowRequest
        {
            Name = "Member Flow",
            SceneIds = [scene.Id],
        });
        createFlowResponse.EnsureSuccessStatusCode();
        var flow = await createFlowResponse.ReadAsJsonAsync<FlowResponse>();

        (await memberClient.GetAsync("/api/workspace/flows")).EnsureSuccessStatusCode();
        (await memberClient.GetAsync($"/api/workspace/flows/{flow.Id}")).EnsureSuccessStatusCode();
        (await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/flows")).EnsureSuccessStatusCode();

        var updateFlowResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/flows/{flow.Id}",
            new UpdateFlowRequest { Name = "Updated Flow" });
        updateFlowResponse.EnsureSuccessStatusCode();

        var addSceneGroupResponse = await memberClient.PostAsJsonAsync(
            "/api/workspace/scene-groups",
            new AttachSceneGroupRequest { SceneId = scene.Id, GroupId = group.Id });
        Assert.Equal(HttpStatusCode.Created, addSceneGroupResponse.StatusCode);

        var setSceneGroupsResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/scene-groups?sceneId={scene.Id}",
            new SetSceneGroupsRequest
            {
                Groups = [new ResourceGroupShareRequest { GroupId = group.Id, IncludeAssets = false }],
            });
        setSceneGroupsResponse.EnsureSuccessStatusCode();

        (await memberClient.GetAsync($"/api/workspace/scene-groups?sceneId={scene.Id}")).EnsureSuccessStatusCode();

        var removeSceneGroupResponse = await memberClient.DeleteAsync(
            $"/api/workspace/scene-groups?sceneId={scene.Id}&groupId={group.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeSceneGroupResponse.StatusCode);

        var addFlowGroupResponse = await memberClient.PostAsJsonAsync(
            "/api/workspace/flow-groups",
            new AttachFlowGroupRequest { FlowId = flow.Id, GroupId = group.Id });
        Assert.Equal(HttpStatusCode.Created, addFlowGroupResponse.StatusCode);

        var setFlowGroupsResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/flow-groups?flowId={flow.Id}",
            new SetFlowGroupsRequest
            {
                Groups = [new ResourceGroupShareRequest { GroupId = group.Id, IncludeAssets = false }],
            });
        setFlowGroupsResponse.EnsureSuccessStatusCode();

        (await memberClient.GetAsync($"/api/workspace/flow-groups?flowId={flow.Id}")).EnsureSuccessStatusCode();

        var removeFlowGroupResponse = await memberClient.DeleteAsync(
            $"/api/workspace/flow-groups?flowId={flow.Id}&groupId={group.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeFlowGroupResponse.StatusCode);

        var deleteFlowResponse = await memberClient.DeleteAsync($"/api/workspace/flows/{flow.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteFlowResponse.StatusCode);

        var deleteSceneResponse = await memberClient.DeleteAsync($"/api/workspace/scenes/{scene.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteSceneResponse.StatusCode);
    }

    [Fact]
    public async Task MemberWithoutLoadScenesAndFlows_CannotReadScenesOrFlows()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"assets-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Scene Flow Deny Gate", "Scene and flow parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadAssets"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Asset Only",
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

        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/api/workspace/scenes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/workspace/scenes/{scene.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/api/workspace/flows")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/workspace/flows/{flow.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/scenes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/flows")).StatusCode);
    }
}
