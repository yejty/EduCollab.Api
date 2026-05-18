using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;
using EduCollab.Contracts.Workspaces;

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

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest
        {
            Name = "Edu Workspace",
            Description = "Integration tests",
        });

        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/invite", new InviteUserRequest
        {
            Email = memberEmail,
        });

        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);

        var acceptResponse = await memberClient.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/invite/{invitationToken}/accept", new RegisterUserRequest
        {
            FirstName = "Member",
            LastName = "User",
            Email = memberEmail,
            Password = memberPassword,
            ConfirmPassword = memberPassword,
        });

        Assert.Equal(HttpStatusCode.Created, acceptResponse.StatusCode);
        var membership = await acceptResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var getMemberResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/users/{membership.UserId}");
        getMemberResponse.EnsureSuccessStatusCode();

        var getMembersResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/users");
        getMembersResponse.EnsureSuccessStatusCode();
        var members = await getMembersResponse.ReadAsJsonAsync<WorkspaceMembersResponse>();
        Assert.Equal(2, members.Members.Count);

        var promoteResponse = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/users/{membership.UserId}", new UpdateWorkspaceMemberRequest
        {
            UserId = membership.UserId,
            Role = WorkspaceRole.Admin,
        });

        promoteResponse.EnsureSuccessStatusCode();
        var promoted = await promoteResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal(WorkspaceRole.Admin, promoted.Role);

        var memberWorkspaceResponse = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}");
        memberWorkspaceResponse.EnsureSuccessStatusCode();
        var memberWorkspace = await memberWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(WorkspaceRole.Admin, memberWorkspace.CurrentUserRole);

        var updateWorkspaceResponse = await memberClient.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", new UpdateWorkspaceRequest
        {
            Name = "Updated Workspace",
            Description = "Updated by admin",
        });

        updateWorkspaceResponse.EnsureSuccessStatusCode();
        var updatedWorkspace = await updateWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal("Updated Workspace", updatedWorkspace.Name);

        var removeMemberResponse = await ownerClient.DeleteAsync($"/api/workspaces/{workspace.Id}/users/{membership.UserId}");
        Assert.Equal(HttpStatusCode.NoContent, removeMemberResponse.StatusCode);
    }
}
