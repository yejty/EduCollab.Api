using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class UserParameterAuthorizationIntegrationTests
{
    [Fact]
    public async Task SeeUsersTabOnly_CanListAndGetUsers_ButCannotManageMembersOrInvite()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "See Users Tab Gate", "User parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["seeUsersTab", "loadScenes", "loadFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "See Users",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<UserResponse>();

        var listMembersResponse = await memberClient.GetAsync("/api/workspace/users");
        listMembersResponse.EnsureSuccessStatusCode();

        var getMemberResponse = await memberClient.GetAsync($"/api/workspace/users/{owner.Id}");
        getMemberResponse.EnsureSuccessStatusCode();

        var getUserResponse = await memberClient.GetAsync($"/api/users/{owner.Id}");
        getUserResponse.EnsureSuccessStatusCode();

        var inviteAttempt = await memberClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = $"blocked-{Guid.NewGuid():N}@example.com",
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes", "loadFlows"],
        });
        Assert.Equal(HttpStatusCode.Forbidden, inviteAttempt.StatusCode);

        var updateAttempt = await memberClient.PutAsJsonAsync(
            $"/api/workspace/users/{owner.Id}",
            new UpdateWorkspaceMemberRequest { Parameters = ["loadScenes", "loadFlows"] });
        Assert.Equal(HttpStatusCode.Forbidden, updateAttempt.StatusCode);

        var deleteAttempt = await memberClient.DeleteAsync($"/api/workspace/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteAttempt.StatusCode);
    }

    [Fact]
    public async Task InviteUsersOnly_CanListGetUsers_AndManageMembers()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var inviterClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var inviterEmail = $"inviter-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Invite Users Gate", "User parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteInviterResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = inviterEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["inviteUsers"],
        });
        inviteInviterResponse.EnsureSuccessStatusCode();

        var inviterToken = factory.GetInvitationToken(inviterEmail);
        var acceptInviterResponse = await inviterClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{inviterToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Invite Only",
                Email = inviterEmail,
                Password = password,
            });
        acceptInviterResponse.EnsureSuccessStatusCode();

        var inviterTokens = await inviterClient.LoginAsync(inviterEmail, password);
        inviterClient.SetBearerToken(inviterTokens.AccessToken);

        factory.EmailSender.Clear();
        var inviteViewerResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes", "loadFlows"],
        });
        inviteViewerResponse.EnsureSuccessStatusCode();

        var viewerToken = factory.GetInvitationToken(viewerEmail);
        var acceptViewerResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{viewerToken}/accept",
            new RegisterUserRequest
            {
                FullName = "View Only",
                Email = viewerEmail,
                Password = password,
            });
        acceptViewerResponse.EnsureSuccessStatusCode();

        var viewerTokens = await viewerClient.LoginAsync(viewerEmail, password);
        viewerClient.SetBearerToken(viewerTokens.AccessToken);

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<UserResponse>();

        var viewerMeResponse = await viewerClient.GetAsync("/api/users/me");
        viewerMeResponse.EnsureSuccessStatusCode();
        var viewer = await viewerMeResponse.ReadAsJsonAsync<UserResponse>();

        (await inviterClient.GetAsync("/api/workspace/users")).EnsureSuccessStatusCode();
        (await inviterClient.GetAsync($"/api/workspace/users/{owner.Id}")).EnsureSuccessStatusCode();
        (await inviterClient.GetAsync($"/api/users/{viewer.Id}")).EnsureSuccessStatusCode();

        factory.EmailSender.Clear();
        var newInviteEmail = $"invited-{Guid.NewGuid():N}@example.com";
        var createInviteResponse = await inviterClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = newInviteEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["inviteUsers", "loadScenes", "loadFlows"],
        });
        Assert.Equal(HttpStatusCode.Forbidden, createInviteResponse.StatusCode);

        factory.EmailSender.Clear();
        var allowedInviteResponse = await inviterClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = newInviteEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["inviteUsers"],
        });
        allowedInviteResponse.EnsureSuccessStatusCode();

        var updateViewerResponse = await inviterClient.PutAsJsonAsync(
            $"/api/workspace/users/{viewer.Id}",
            new UpdateWorkspaceMemberRequest { Parameters = ["inviteUsers", "loadScenes", "loadFlows"] });
        Assert.Equal(HttpStatusCode.Forbidden, updateViewerResponse.StatusCode);

        var allowedUpdateResponse = await inviterClient.PutAsJsonAsync(
            $"/api/workspace/users/{viewer.Id}",
            new UpdateWorkspaceMemberRequest { Parameters = ["inviteUsers"] });
        allowedUpdateResponse.EnsureSuccessStatusCode();

        var deleteViewerResponse = await inviterClient.DeleteAsync($"/api/workspace/users/{viewer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteViewerResponse.StatusCode);
    }

    [Fact]
    public async Task MemberWithoutUserPresets_CannotListOrGetWorkspaceUsers()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"creator-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "No User Parameter Gate", "User parameter authorization");
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
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<UserResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/api/workspace/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/workspace/users/{owner.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync($"/api/users/{owner.Id}")).StatusCode);
    }
}
