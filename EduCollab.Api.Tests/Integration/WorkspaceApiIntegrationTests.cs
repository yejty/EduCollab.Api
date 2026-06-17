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

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync("/api/workspace", new CreateWorkspaceRequest
        {
            Name = "Edu Workspace",
            Description = "Integration tests",
        });

        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();

        var listAsOwnerResponse = await ownerClient.GetAsync("/api/admin/workspaces");
        Assert.Equal(HttpStatusCode.Forbidden, listAsOwnerResponse.StatusCode);
        var forbiddenBody = await listAsOwnerResponse.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("Insufficient rights.", forbiddenBody.ErrorDescription);

        using var adminClient = factory.CreateClient();
        var adminTokens = await adminClient.LoginAsync("admin@educollab.local", "Admin123!");
        adminClient.SetBearerToken(adminTokens.AccessToken);

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

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync("/api/workspace", new CreateWorkspaceRequest
        {
            Name = "Existing User Workspace",
            Description = "Invitation join test",
        });
        createWorkspaceResponse.EnsureSuccessStatusCode();

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
}
