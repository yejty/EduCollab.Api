using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Groups;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class GroupPresetAuthorizationIntegrationTests
{
    [Fact]
    public async Task AddGroupsOnly_CanManageGroupsAndMembers()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"grouper-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Add Groups Gate", "Group preset authorization");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Presets = ["addGroups", "loadScenesAndFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Group",
                LastName = "Manager",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var createGroupResponse = await memberClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Member Team",
            Description = "Created by addGroups member",
        });
        createGroupResponse.EnsureSuccessStatusCode();
        var group = await createGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var updateGroupResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/groups/{group.Id}",
            new UpdateGroupRequest { Name = "Updated Team", Description = "Renamed" });
        updateGroupResponse.EnsureSuccessStatusCode();

        var addOwnerResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)owner.Id) });
        addOwnerResponse.EnsureSuccessStatusCode();

        var listMembersResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/users");
        listMembersResponse.EnsureSuccessStatusCode();
        var members = await listMembersResponse.ReadAsJsonAsync<GroupMembersResponse>();
        Assert.Contains(members.Members, member => member.UserId == owner.Id);

        var getOwnerMemberResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/users/{owner.Id}");
        getOwnerMemberResponse.EnsureSuccessStatusCode();

        var removeOwnerResponse = await memberClient.DeleteAsync($"/api/workspace/groups/{group.Id}/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeOwnerResponse.StatusCode);

        var deleteGroupResponse = await memberClient.DeleteAsync($"/api/workspace/groups/{group.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteGroupResponse.StatusCode);
    }

    [Fact]
    public async Task AddGroupsOnly_NotInGroup_CannotAddOrRemoveMembers()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"grouper-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Add Groups Membership Gate", "Group preset authorization");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Presets = ["addGroups", "loadScenesAndFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Group",
                LastName = "Manager",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Owner Team" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var memberMeResponse = await memberClient.GetAsync("/api/users/me");
        memberMeResponse.EnsureSuccessStatusCode();
        var member = await memberMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var addOwnerAttempt = await memberClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)owner.Id) });
        Assert.Equal(HttpStatusCode.Forbidden, addOwnerAttempt.StatusCode);

        var addSelfAttempt = await memberClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) });
        Assert.Equal(HttpStatusCode.Forbidden, addSelfAttempt.StatusCode);

        var listMembersAttempt = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/users");
        Assert.Equal(HttpStatusCode.Forbidden, listMembersAttempt.StatusCode);

        var removeOwnerAttempt = await memberClient.DeleteAsync($"/api/workspace/groups/{group.Id}/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, removeOwnerAttempt.StatusCode);
    }

    [Fact]
    public async Task MemberWithoutAddGroups_CannotManageGroupsOrMembers()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"creator-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "No Add Groups Gate", "Group preset authorization");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Presets = WorkspacePresetTestHelpers.PresetsForRole(WorkspaceRole.Creator),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Creator",
                LastName = "Member",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Owner Team" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var memberMeResponse = await memberClient.GetAsync("/api/users/me");
        memberMeResponse.EnsureSuccessStatusCode();
        var member = await memberMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var ownerMeResponse = await ownerClient.GetAsync("/api/users/me");
        ownerMeResponse.EnsureSuccessStatusCode();
        var owner = await ownerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var createGroupAttempt = await memberClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Blocked" });
        Assert.Equal(HttpStatusCode.Forbidden, createGroupAttempt.StatusCode);

        var updateGroupAttempt = await memberClient.PutAsJsonAsync(
            $"/api/workspace/groups/{group.Id}",
            new UpdateGroupRequest { Name = "Blocked Rename" });
        Assert.Equal(HttpStatusCode.Forbidden, updateGroupAttempt.StatusCode);

        var deleteGroupAttempt = await memberClient.DeleteAsync($"/api/workspace/groups/{group.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteGroupAttempt.StatusCode);

        var addMemberAttempt = await memberClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)owner.Id) });
        Assert.Equal(HttpStatusCode.Forbidden, addMemberAttempt.StatusCode);

        var removeMemberAttempt = await memberClient.DeleteAsync($"/api/workspace/groups/{group.Id}/users/{owner.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, removeMemberAttempt.StatusCode);
    }
}
