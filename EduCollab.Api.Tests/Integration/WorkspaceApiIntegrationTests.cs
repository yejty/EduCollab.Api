using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Tests.Integration;

public sealed class WorkspaceApiIntegrationTests
{
    [Fact]
    public async Task Workspace_Invitation_And_MembershipFlow_Works()
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

        using var adminClient = factory.CreateClient();
        var adminTokens = await adminClient.LoginAsync("admin@educollab.local", "Admin123!");
        adminClient.SetBearerToken(adminTokens.AccessToken);

        var workspace = await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Edu Workspace",
            "Integration tests");

        var listAsOwnerResponse = await ownerClient.GetAsync("/api/admin/workspaces");
        Assert.Equal(HttpStatusCode.Forbidden, listAsOwnerResponse.StatusCode);
        var forbiddenBody = await listAsOwnerResponse.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("Insufficient rights.", forbiddenBody.Detail);

        var listAsAdminResponse = await adminClient.GetAsync("/api/admin/workspaces");
        listAsAdminResponse.EnsureSuccessStatusCode();
        var workspacesList = await listAsAdminResponse.ReadAsJsonAsync<WorkspacesResponse>();
        Assert.Contains(workspacesList.Workspaces, w => w.Id == workspace.Id);

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Manager",
        });

        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);

        var acceptResponse = await memberClient.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
        {
            FirstName = "Member",
            LastName = "User",
            Email = memberEmail,
            Password = memberPassword,
        });

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var membership = await acceptResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("Manager", membership.Role);

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var getMemberResponse = await ownerClient.GetAsync($"/api/workspace/users/{membership.UserId}");
        getMemberResponse.EnsureSuccessStatusCode();

        var getMembersResponse = await ownerClient.GetAsync("/api/workspace/users");
        getMembersResponse.EnsureSuccessStatusCode();
        var members = await getMembersResponse.ReadAsJsonAsync<WorkspaceMembersResponse>();
        Assert.Equal(2, members.Members.Count);

        var promoteResponse = await ownerClient.PutAsJsonAsync($"/api/workspace/users/{membership.UserId}", new UpdateWorkspaceMemberRequest
        {
            Role = "Creator",
        });

        promoteResponse.EnsureSuccessStatusCode();
        var promoted = await promoteResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("Creator", promoted.Role);

        var memberWorkspaceResponse = await memberClient.GetAsync("/api/workspace");
        memberWorkspaceResponse.EnsureSuccessStatusCode();
        var memberWorkspace = await memberWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal("Manager", memberWorkspace.CurrentUserRole);

        var updateWorkspaceResponse = await memberClient.PutAsJsonAsync("/api/workspace", new UpdateWorkspaceRequest
        {
            Name = "Updated Workspace",
            Description = "Updated by manager",
        });

        Assert.Equal(HttpStatusCode.Forbidden, updateWorkspaceResponse.StatusCode);

        var removeMemberResponse = await ownerClient.DeleteAsync($"/api/workspace/users/{membership.UserId}");
        Assert.Equal(HttpStatusCode.NoContent, removeMemberResponse.StatusCode);
    }

    [Fact]
    public async Task ExistingUser_CanJoinWorkspace_FromInvitation_WithAssignedRole()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var existingUserClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Existing User Workspace",
            "Invitation join test");

        await existingUserClient.RegisterAndConfirmAsync(factory, "Existing", "User", memberEmail, memberPassword);

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Manager",
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var memberTokens = await existingUserClient.LoginAsync(memberEmail, memberPassword);
        existingUserClient.SetBearerToken(memberTokens.AccessToken);

        var joinResponse = await existingUserClient.PostAsync($"/api/workspace-invitations/{invitationToken}/join", null);
        joinResponse.EnsureSuccessStatusCode();
        var membership = await joinResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("Manager", membership.Role);

        var workspaceResponse = await existingUserClient.GetAsync("/api/workspace");
        workspaceResponse.EnsureSuccessStatusCode();
        var workspace = await workspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal("Manager", workspace.CurrentUserRole);
    }

    [Fact]
    public async Task ExistingUser_CanJoinSecondWorkspace_AndSwitchActiveWorkspace()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var firstOwnerClient = factory.CreateClient();
        using var secondOwnerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string memberPassword = "Member123!";
        const string ownerPassword = "Owner123!";

        var firstOwnerEmail = $"owner1-{Guid.NewGuid():N}@example.com";
        var firstOwnerTokens = await firstOwnerClient.RegisterAndConfirmAsync(factory, "First", "Owner", firstOwnerEmail, ownerPassword);
        firstOwnerClient.SetBearerToken(firstOwnerTokens.AccessToken);
        var firstWorkspace = await firstOwnerClient.CreateApprovedWorkspaceAsync(
            factory,
            firstOwnerEmail,
            "First Workspace",
            "Primary membership");

        factory.EmailSender.Clear();

        var firstInviteResponse = await firstOwnerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Viewer",
        });
        firstInviteResponse.EnsureSuccessStatusCode();

        var firstInvitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{firstInvitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Multi",
                LastName = "Member",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var secondOwnerEmail = $"owner2-{Guid.NewGuid():N}@example.com";
        var secondOwnerTokens = await secondOwnerClient.RegisterAndConfirmAsync(factory, "Second", "Owner", secondOwnerEmail, ownerPassword);
        secondOwnerClient.SetBearerToken(secondOwnerTokens.AccessToken);
        await secondOwnerClient.CreateApprovedWorkspaceAsync(
            factory,
            secondOwnerEmail,
            "Second Workspace",
            "Secondary membership target");

        factory.EmailSender.Clear();

        var secondInviteResponse = await secondOwnerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Creator",
        });
        secondInviteResponse.EnsureSuccessStatusCode();

        var secondInvitationToken = factory.GetInvitationToken(memberEmail);
        var joinSecondResponse = await memberClient.PostAsync($"/api/workspace-invitations/{secondInvitationToken}/join", null);
        joinSecondResponse.EnsureSuccessStatusCode();
        var secondMembership = await joinSecondResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("Creator", secondMembership.Role);

        var workspacesResponse = await memberClient.GetAsync("/api/users/me/workspaces");
        workspacesResponse.EnsureSuccessStatusCode();
        var workspaces = await workspacesResponse.ReadAsJsonAsync<UserWorkspacesResponse>();
        Assert.Equal(2, workspaces.Workspaces.Count);
        Assert.Contains(workspaces.Workspaces, workspace => workspace.WorkspaceName == "Second Workspace" && workspace.IsActive);
        Assert.Contains(workspaces.Workspaces, workspace => workspace.WorkspaceName == "First Workspace" && !workspace.IsActive);

        var switchResponse = await memberClient.PutAsJsonAsync("/api/users/me/active-workspace", new SetActiveWorkspaceRequest
        {
            WorkspaceId = firstWorkspace.Id,
        });
        switchResponse.EnsureSuccessStatusCode();
        var me = await switchResponse.ReadAsJsonAsync<UserResponse>();
        Assert.Equal(firstWorkspace.Id, me.WorkspaceId);

        var currentWorkspaceResponse = await memberClient.GetAsync("/api/workspace");
        currentWorkspaceResponse.EnsureSuccessStatusCode();
        var currentWorkspace = await currentWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(firstWorkspace.Id, currentWorkspace.Id);
        Assert.Equal("Viewer", currentWorkspace.CurrentUserRole);
    }
}
