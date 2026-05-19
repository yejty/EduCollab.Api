using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Models.Workspaces;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Tests;

public sealed class WorkspacesControllerEndpointTests
{
    [Fact]
    public async Task InviteToWorkspace_ReturnsOk_WhenInvitationSucceeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 31);

        var response = await client.PostAsJsonAsync("/api/workspaces/5/invite", new InviteUserRequest
        {
            Email = "invitee@example.com",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InviteToWorkspace_ReturnsForbidden_WhenServiceThrowsAccessDenied()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.InviteUserToWorkspaceAsyncHandler = (_, _, _) => throw new AccessDeniedException("Forbidden.");

        using var client = factory.CreateClient(userId: 31);

        var response = await client.PostAsJsonAsync("/api/workspaces/5/invite", new InviteUserRequest
        {
            Email = "invitee@example.com",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("forbidden", body.Error);
    }

    [Fact]
    public async Task CreateWorkspaceUser_ReturnsCreated_WhenInvitationIsAccepted()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.CreateUserInWorkspaceAsyncHandler = (_, user, _, _, _) =>
        {
            user.Id = 41;
            return Task.FromResult(true);
        };
        factory.WorkspaceService.GetWorkspaceMemberAsyncHandler = (_, userId, _) => Task.FromResult<WorkspaceMember?>(new WorkspaceMember
        {
            UserId = userId,
            WorkspaceId = 5,
            Role = WorkspaceRole.Member,
            JoinedAtUtc = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc),
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/workspaces/5/invite/token-123/accept", new RegisterUserRequest
        {
            FirstName = "Invited",
            LastName = "User",
            Email = "invitee@example.com",
            Password = "Pass123!",
            ConfirmPassword = "Pass123!",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal(41, body.UserId);
        Assert.Equal("Member", body.Role);
    }

    [Fact]
    public async Task GetWorkspaceMembers_ReturnsNotFound_WhenWorkspaceDoesNotExist()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.GetWorkspaceAsyncHandler = (_, _) => Task.FromResult<Workspace?>(null);

        using var client = factory.CreateClient(userId: 32);

        var response = await client.GetAsync("/api/workspaces/99/users");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsCreated_WithOwnerRole()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.CreateWorkspaceAsyncHandler = (workspace, _) =>
        {
            workspace.Id = 55;
            return Task.FromResult(true);
        };

        using var client = factory.CreateClient(userId: 33);

        var response = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest
        {
            Name = "Alpha",
            Description = "Team workspace",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(55, body.Id);
        Assert.Equal("Owner", body.CurrentUserRole);
    }

    [Fact]
    public async Task GetWorkspaces_ReturnsOk_WithAllWorkspaces()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.GetWorkspacesAsyncHandler = _ => Task.FromResult(new List<Workspace>
        {
            new() { Id = 55, Name = "Alpha", Description = "First" },
            new() { Id = 56, Name = "Beta" },
        });

        using var client = factory.CreateClient(userId: 33);

        var response = await client.GetAsync("/api/workspaces");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspacesResponse>();
        Assert.Equal(2, body.Workspaces.Count);
        Assert.Equal("Alpha", body.Workspaces[0].Name);
        Assert.Null(body.Workspaces[0].CurrentUserRole);
    }

    [Fact]
    public async Task GetWorkspace_ReturnsWorkspace_WithCurrentUserRole()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.GetWorkspaceAsyncHandler = (id, _) => Task.FromResult<Workspace?>(new Workspace
        {
            Id = id,
            Name = "Alpha",
        });
        factory.WorkspaceService.GetCurrentUserWorkspaceMemberAsyncHandler = (_, _) => Task.FromResult<WorkspaceMember?>(new WorkspaceMember
        {
            UserId = 33,
            WorkspaceId = 55,
            Role = WorkspaceRole.Admin,
        });

        using var client = factory.CreateClient(userId: 33);

        var response = await client.GetAsync("/api/workspaces/55");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(55, body.Id);
        Assert.Equal("Admin", body.CurrentUserRole);
    }

    [Fact]
    public async Task UpdateWorkspace_ReturnsBadRequest_WhenServiceReturnsFalse()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.UpdateWorkspaceAsyncHandler = (_, _) => Task.FromResult(false);

        using var client = factory.CreateClient(userId: 34);

        var response = await client.PutAsJsonAsync("/api/workspaces/55", new UpdateWorkspaceRequest
        {
            Name = "Renamed",
            Description = "Updated description",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("update_failed", body.Error);
    }

    [Fact]
    public async Task DeleteWorkspace_ReturnsNoContent_WhenServiceReturnsTrue()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.DeleteWorkspaceAsyncHandler = (_, _) => Task.FromResult(true);

        using var client = factory.CreateClient(userId: 35);

        var response = await client.DeleteAsync("/api/workspaces/55");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkspaceMember_ReturnsNoContent_WhenRemovalSucceeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 36);

        var response = await client.DeleteAsync("/api/workspaces/55/users/77");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkspaceMember_ReturnsUpdatedMember_WhenServiceReturnsMember()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.UpdateWorkspaceMemberAsyncHandler = (_, _, member, _) => Task.FromResult<WorkspaceMember?>(new WorkspaceMember
        {
            UserId = member.UserId,
            WorkspaceId = member.WorkspaceId,
            Role = member.Role,
            JoinedAtUtc = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc),
        });

        using var client = factory.CreateClient(userId: 37);

        var response = await client.PutAsJsonAsync("/api/workspaces/55/users/77", new UpdateWorkspaceMemberRequest
        {
            Role = "Admin",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal(77, body.UserId);
        Assert.Equal("Admin", body.Role);
    }
}
