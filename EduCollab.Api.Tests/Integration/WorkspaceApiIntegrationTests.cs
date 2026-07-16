using System.Net;
using EduCollab.Application.Models;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
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

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        using var adminClient = factory.CreateClient();
        var adminTokens = await adminClient.LoginAsync("admin@educollab.local", "Admin123!");
        adminClient.SetBearerToken(adminTokens.AccessToken);

        var workspace = await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Edu Workspace",
            "Integration tests");
        var invitationGroup = await ownerClient.CreateGroupAsync();

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
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Manager),
        });

        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);

        var acceptResponse = await memberClient.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
        {
            FullName = "Member User",
            Email = memberEmail,
            Password = memberPassword,
        });

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var membership = await acceptResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("manager", membership.Role);
        Assert.Contains("inviteUsers", membership.Parameters);

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
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Creator),
        });

        promoteResponse.EnsureSuccessStatusCode();
        var promoted = await promoteResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("creator", promoted.Role);
        Assert.Contains("addAssets", promoted.Parameters);

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

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Existing User Workspace",
            "Invitation join test");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        await existingUserClient.RegisterAndConfirmAsync(factory, "Existing User", memberEmail, memberPassword);

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Manager),
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var memberTokens = await existingUserClient.LoginAsync(memberEmail, memberPassword);
        existingUserClient.SetBearerToken(memberTokens.AccessToken);

        var joinResponse = await existingUserClient.PostAsync($"/api/workspace-invitations/{invitationToken}/join", null);
        joinResponse.EnsureSuccessStatusCode();
        var membership = await joinResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("manager", membership.Role);
        Assert.Contains("inviteUsers", membership.Parameters);

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
        var firstOwnerTokens = await firstOwnerClient.RegisterAndConfirmAsync(factory, "First Owner", firstOwnerEmail, ownerPassword);
        firstOwnerClient.SetBearerToken(firstOwnerTokens.AccessToken);
        var firstWorkspace = await firstOwnerClient.CreateApprovedWorkspaceAsync(
            factory,
            firstOwnerEmail,
            "First Workspace",
            "Primary membership");
        var firstInvitationGroup = await firstOwnerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var firstInviteResponse = await firstOwnerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = firstInvitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Viewer),
        });
        firstInviteResponse.EnsureSuccessStatusCode();

        var firstInvitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{firstInvitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Multi Member",
                Email = memberEmail,
                Password = memberPassword,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var secondOwnerEmail = $"owner2-{Guid.NewGuid():N}@example.com";
        var secondOwnerTokens = await secondOwnerClient.RegisterAndConfirmAsync(factory, "Second Owner", secondOwnerEmail, ownerPassword);
        secondOwnerClient.SetBearerToken(secondOwnerTokens.AccessToken);
        await secondOwnerClient.CreateApprovedWorkspaceAsync(
            factory,
            secondOwnerEmail,
            "Second Workspace",
            "Secondary membership target");
        var secondInvitationGroup = await secondOwnerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var secondInviteResponse = await secondOwnerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = secondInvitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Creator),
        });
        secondInviteResponse.EnsureSuccessStatusCode();

        var secondInvitationToken = factory.GetInvitationToken(memberEmail);
        var joinSecondResponse = await memberClient.PostAsync($"/api/workspace-invitations/{secondInvitationToken}/join", null);
        joinSecondResponse.EnsureSuccessStatusCode();
        var secondMembership = await joinSecondResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("creator", secondMembership.Role);
        Assert.Contains("addAssets", secondMembership.Parameters);

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
        var secondWorkspaceId = workspaces.Workspaces.Single(w => w.WorkspaceName == "Second Workspace").WorkspaceId;
        var me = await switchResponse.ReadAsJsonAsync<UserResponse>();
        Assert.Equal([firstWorkspace.Id, secondWorkspaceId], me.WorkspaceIds.OrderBy(id => id));

        var currentWorkspaceResponse = await memberClient.GetAsync("/api/workspace");
        currentWorkspaceResponse.EnsureSuccessStatusCode();
        var currentWorkspace = await currentWorkspaceResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(firstWorkspace.Id, currentWorkspace.Id);
        Assert.Equal("Viewer", currentWorkspace.CurrentUserRole);
    }

    [Fact]
    public async Task Workspace_AllowsMultipleOwners_AndLastOwnerCannotLeave()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var secondOwnerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var secondOwnerEmail = $"owner2-{Guid.NewGuid():N}@example.com";
        const string password = "Owner123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Multi Owner Workspace",
            "Multiple owners allowed");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = secondOwnerEmail,
            GroupId = invitationGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Owner),
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(secondOwnerEmail);
        var acceptResponse = await secondOwnerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Second Owner",
                Email = secondOwnerEmail,
                Password = password,
            });
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var secondOwnerMembership = await acceptResponse.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal("owner", secondOwnerMembership.Role);
        Assert.Contains("editWorkspace", secondOwnerMembership.Parameters);

        var membersResponse = await ownerClient.GetAsync("/api/workspace/users");
        membersResponse.EnsureSuccessStatusCode();
        var members = await membersResponse.ReadAsJsonAsync<WorkspaceMembersResponse>();
        Assert.Equal(2, members.Members.Count(member => member.Role == "owner"));

        var secondOwnerTokens = await secondOwnerClient.LoginAsync(secondOwnerEmail, password);
        secondOwnerClient.SetBearerToken(secondOwnerTokens.AccessToken);

        var leaveResponse = await secondOwnerClient.DeleteAsync($"/api/workspace/users/{secondOwnerMembership.UserId}");
        Assert.Equal(HttpStatusCode.NoContent, leaveResponse.StatusCode);

        var remainingMembersResponse = await ownerClient.GetAsync("/api/workspace/users");
        remainingMembersResponse.EnsureSuccessStatusCode();
        var remainingMembers = await remainingMembersResponse.ReadAsJsonAsync<WorkspaceMembersResponse>();
        Assert.Single(remainingMembers.Members);
        Assert.Equal("owner", remainingMembers.Members[0].Role);

        var soleOwnerId = remainingMembers.Members[0].UserId;
        var soleOwnerLeaveResponse = await ownerClient.DeleteAsync($"/api/workspace/users/{soleOwnerId}");
        Assert.Equal(HttpStatusCode.BadRequest, soleOwnerLeaveResponse.StatusCode);
    }

    [Fact]
    public async Task Invite_PlacesUserInGroup_AndBlocksInvitingIntoInaccessibleGroup()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();
        using var inviteeClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var managerEmail = $"manager-{Guid.NewGuid():N}@example.com";
        var inviteeEmail = $"invitee-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);
        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Invite Group Scope", "Group-scoped invites");

        var parentGroup = await ownerClient.CreateGroupAsync("Parent");
        var otherRoot = await ownerClient.CreateGroupAsync("OtherRoot");

        factory.EmailSender.Clear();
        var inviteManager = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = managerEmail,
            GroupId = parentGroup.Id,
            Parameters = WorkspaceParameterTestHelpers.ParametersForRole(WorkspaceRole.Manager),
        });
        inviteManager.EnsureSuccessStatusCode();

        var managerToken = factory.GetInvitationToken(managerEmail);
        var acceptManager = await managerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{managerToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Manager",
                Email = managerEmail,
                Password = password,
            });
        acceptManager.EnsureSuccessStatusCode();
        var managerMembership = await acceptManager.ReadAsJsonAsync<WorkspaceMemberResponse>();

        var managerInParent = await ownerClient.GetAsync(
            $"/api/workspace/groups/{parentGroup.Id}/users/{managerMembership.UserId}");
        managerInParent.EnsureSuccessStatusCode();

        var managerTokens = await managerClient.LoginAsync(managerEmail, password);
        managerClient.SetBearerToken(managerTokens.AccessToken);

        factory.EmailSender.Clear();
        var inviteOutsideAccess = await managerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = inviteeEmail,
            GroupId = otherRoot.Id,
            Parameters = ["loadScenes", "loadFlows"],
        });
        Assert.Equal(HttpStatusCode.Forbidden, inviteOutsideAccess.StatusCode);

        var inviteIntoParent = await managerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = inviteeEmail,
            GroupId = parentGroup.Id,
            Parameters = ["loadScenes", "loadFlows"],
        });
        inviteIntoParent.EnsureSuccessStatusCode();

        var inviteeToken = factory.GetInvitationToken(inviteeEmail);
        var acceptInvitee = await inviteeClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{inviteeToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Invitee",
                Email = inviteeEmail,
                Password = password,
            });
        acceptInvitee.EnsureSuccessStatusCode();
        var inviteeMembership = await acceptInvitee.ReadAsJsonAsync<WorkspaceMemberResponse>();

        var inviteeInParent = await ownerClient.GetAsync(
            $"/api/workspace/groups/{parentGroup.Id}/users/{inviteeMembership.UserId}");
        inviteeInParent.EnsureSuccessStatusCode();
    }
}
