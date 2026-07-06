using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Groups;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PresetAuthorizationIntegrationTests
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

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Custom Preset Workspace",
            "Preset authorization integration test");

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Presets = ["addAssets", "loadScenes"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Custom",
                LastName = "Member",
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
            Presets = ["loadScenes"],
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

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Viewer Asset Gate", "Preset test");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            Presets = WorkspacePresetTestHelpers.PresetsForRole(WorkspaceRole.Viewer),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(viewerEmail);
        var acceptResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "View",
                LastName = "Only",
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
}
