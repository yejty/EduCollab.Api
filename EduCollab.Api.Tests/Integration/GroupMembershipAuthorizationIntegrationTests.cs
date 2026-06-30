using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class GroupMembershipAuthorizationIntegrationTests
{
    [Fact]
    public async Task WorkspaceManager_NotInGroup_CannotAddMembers_IncludingSelf()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var managerEmail = $"manager-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Group Membership Auth Workspace",
            "Group membership authorization test");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = managerEmail,
            Role = "Manager",
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(managerEmail);
        var acceptResponse = await managerClient.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
        {
            FirstName = "Workspace",
            LastName = "Manager",
            Email = managerEmail,
            Password = password,
        });
        acceptResponse.EnsureSuccessStatusCode();

        var managerTokens = await managerClient.LoginAsync(managerEmail, password);
        managerClient.SetBearerToken(managerTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Owner Group",
            Description = "Group created by owner",
        });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var managerMeResponse = await managerClient.GetAsync("/api/users/me");
        managerMeResponse.EnsureSuccessStatusCode();
        var managerUser = await managerMeResponse.ReadAsJsonAsync<UserResponse>();

        var addSelfResponse = await managerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)managerUser.Id) });
        Assert.Equal(HttpStatusCode.Forbidden, addSelfResponse.StatusCode);
        var forbiddenBody = await addSelfResponse.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("forbidden", forbiddenBody.Error);
    }

    [Fact]
    public async Task WorkspaceManager_InGroup_CanAddMembers()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var managerEmail = $"manager-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Group Membership Auth Workspace",
            "Group membership authorization test");

        foreach (var (email, role, client, first, last) in new[]
        {
            (managerEmail, "Manager", managerClient, "Workspace", "Manager"),
            (viewerEmail, "Viewer", viewerClient, "Workspace", "Viewer"),
        })
        {
            factory.EmailSender.Clear();
            var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
            {
                Email = email,
                Role = role,
            });
            Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

            var invitationToken = factory.GetInvitationToken(email);
            var acceptResponse = await client.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
            {
                FirstName = first,
                LastName = last,
                Email = email,
                Password = password,
            });
            acceptResponse.EnsureSuccessStatusCode();

            var tokens = await client.LoginAsync(email, password);
            client.SetBearerToken(tokens.AccessToken);
        }

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Shared Group",
            Description = "Group for manager membership test",
        });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var managerMeResponse = await managerClient.GetAsync("/api/users/me");
        managerMeResponse.EnsureSuccessStatusCode();
        var managerUser = await managerMeResponse.ReadAsJsonAsync<UserResponse>();

        var addManagerResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)managerUser.Id) });
        addManagerResponse.EnsureSuccessStatusCode();

        var viewerMeResponse = await viewerClient.GetAsync("/api/users/me");
        viewerMeResponse.EnsureSuccessStatusCode();
        var viewerUser = await viewerMeResponse.ReadAsJsonAsync<UserResponse>();

        var addViewerResponse = await managerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)viewerUser.Id) });
        Assert.Equal(HttpStatusCode.Created, addViewerResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateGroup_RejectsParentGroupIdWhenManagerHasNoAccess()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var managerEmail = $"manager-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Group Parent Access Workspace",
            "Group parent access authorization test");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = managerEmail,
            Role = "Manager",
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(managerEmail);
        var acceptResponse = await managerClient.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
        {
            FirstName = "Workspace",
            LastName = "Manager",
            Email = managerEmail,
            Password = password,
        });
        acceptResponse.EnsureSuccessStatusCode();

        var managerTokens = await managerClient.LoginAsync(managerEmail, password);
        managerClient.SetBearerToken(managerTokens.AccessToken);

        var scienceResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Science",
            Description = "Group the manager can access",
        });
        scienceResponse.EnsureSuccessStatusCode();
        var science = await scienceResponse.ReadAsJsonAsync<GroupResponse>();

        var artsResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Arts",
            Description = "Group the manager cannot access",
        });
        artsResponse.EnsureSuccessStatusCode();
        var arts = await artsResponse.ReadAsJsonAsync<GroupResponse>();

        var physicsResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Physics",
            Description = "Science subgroup",
            ParentGroupId = science.Id,
        });
        physicsResponse.EnsureSuccessStatusCode();
        var physics = await physicsResponse.ReadAsJsonAsync<GroupResponse>();

        var managerMeResponse = await managerClient.GetAsync("/api/users/me");
        managerMeResponse.EnsureSuccessStatusCode();
        var managerUser = await managerMeResponse.ReadAsJsonAsync<UserResponse>();

        var addManagerResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{science.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)managerUser.Id) });
        addManagerResponse.EnsureSuccessStatusCode();

        var moveUnderInaccessibleParentResponse = await managerClient.PutAsJsonAsync(
            $"/api/workspace/groups/{physics.Id}",
            new UpdateGroupRequest { ParentGroupId = arts.Id });
        Assert.Equal(HttpStatusCode.Forbidden, moveUnderInaccessibleParentResponse.StatusCode);
        var forbiddenBody = await moveUnderInaccessibleParentResponse.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Contains("member of this group", forbiddenBody.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
