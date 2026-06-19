using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
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

        var response = await client.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = "invitee@example.com",
            Role = "Manager",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InviteToWorkspace_ReturnsForbidden_WhenServiceThrowsAccessDenied()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.InviteUserToCurrentWorkspaceAsyncHandler = (_, _, _) => throw new AccessDeniedException("Forbidden.");

        using var client = factory.CreateClient(userId: 31);

        var response = await client.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = "invitee@example.com",
            Role = "Viewer",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("forbidden", body.Error);
    }

    [Fact]
    public async Task CreateWorkspaceUser_ReturnsCreated_WhenInvitationIsAccepted()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.CreateUserFromInvitationAsyncHandler = (user, _, _, _) =>
        {
            user.Id = 41;
            user.WorkspaceId = 5;
            return Task.FromResult(true);
        };
        factory.WorkspaceService.GetWorkspaceMemberAsyncHandler = (_, userId, _) => Task.FromResult<WorkspaceMember?>(new WorkspaceMember
        {
            UserId = userId,
            WorkspaceId = 5,
            Role = WorkspaceRole.Viewer,
            JoinedAtUtc = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc),
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/workspace-invitations/token-123/accept", new RegisterUserRequest
        {
            FirstName = "Invited",
            LastName = "User",
            Email = "invitee@example.com",
            Password = "Pass123!",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal(41, body.UserId);
        Assert.Equal("Viewer", body.Role);
    }

    [Fact]
    public async Task JoinWorkspaceFromInvitation_ReturnsOk_WhenJoinSucceeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.JoinWorkspaceFromInvitationAsyncHandler = (_, _) => Task.FromResult<WorkspaceMember?>(new WorkspaceMember
        {
            UserId = 42,
            WorkspaceId = 5,
            Role = WorkspaceRole.Manager,
            JoinedAtUtc = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc),
        });

        using var client = factory.CreateClient(userId: 42);

        var response = await client.PostAsync("/api/workspace-invitations/token-123/join", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal(42, body.UserId);
        Assert.Equal("Manager", body.Role);
    }

    [Fact]
    public async Task GetWorkspaceMembers_ReturnsNotFound_WhenWorkspaceDoesNotExist()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.PlatformAdminAuthorization.PlatformAdminUserIds.Add(32);
        factory.WorkspaceService.GetWorkspaceAsyncHandler = (_, _) => Task.FromResult<Workspace?>(null);

        using var client = factory.CreateClient(userId: 32);

        var response = await client.GetAsync("/api/admin/workspaces/99/users");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkspaces_ReturnsForbidden_WhenCallerIsNotPlatformAdmin()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 33);

        var response = await client.GetAsync("/api/admin/workspaces");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("forbidden", body.Error);
        Assert.Equal("Insufficient rights.", body.ErrorDescription);
    }

    [Fact]
    public async Task GetWorkspaces_ReturnsOk_WhenCallerIsPlatformAdmin()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.PlatformAdminAuthorization.PlatformAdminUserIds.Add(33);
        factory.WorkspaceService.GetWorkspacesAsyncHandler = _ => Task.FromResult(new List<Workspace>
        {
            new() { Id = 55, Name = "Alpha", Description = "First" },
            new() { Id = 56, Name = "Beta" },
        });

        using var client = factory.CreateClient(userId: 33);

        var response = await client.GetAsync("/api/admin/workspaces");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspacesResponse>();
        Assert.Equal(2, body.Workspaces.Count);
        Assert.Equal("Alpha", body.Workspaces[0].Name);
        Assert.Null(body.Workspaces[0].CurrentUserRole);
    }

    [Fact]
    public async Task GetWorkspace_ReturnsWorkspace_WhenCallerIsPlatformAdmin()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.PlatformAdminAuthorization.PlatformAdminUserIds.Add(33);
        factory.WorkspaceService.GetWorkspaceAsyncHandler = (id, _) => Task.FromResult<Workspace?>(new Workspace
        {
            Id = id,
            Name = "Alpha",
        });

        using var client = factory.CreateClient(userId: 33);

        var response = await client.GetAsync("/api/admin/workspaces/55");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(55, body.Id);
        Assert.Null(body.CurrentUserRole);
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsCreated_WithOwnerRole()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.CreateWorkspaceAsyncHandler = (workspace, _, _) =>
        {
            workspace.Id = 55;
            return Task.FromResult(true);
        };

        using var client = factory.CreateClient(userId: 33);

        var response = await client.PostAsJsonAsync("/api/workspace", new CreateWorkspaceRequest
        {
            Name = "Alpha",
            Description = "Team workspace",
            ApprovalToken = "approval-token-123",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal(55, body.Id);
        Assert.Equal("Owner", body.CurrentUserRole);
    }

    [Fact]
    public async Task UpdateWorkspace_ReturnsBadRequest_WhenServiceReturnsFalse()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.UpdateCurrentWorkspaceAsyncHandler = (_, _) => Task.FromResult(false);

        using var client = factory.CreateClient(userId: 34);

        var response = await client.PutAsJsonAsync("/api/workspace", new UpdateWorkspaceRequest
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

        var response = await client.DeleteAsync("/api/workspace");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkspaceMember_ReturnsNoContent_WhenRemovalSucceeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 36);

        var response = await client.DeleteAsync("/api/workspace/users/77");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkspaceMember_ReturnsUpdatedMember_WhenServiceReturnsMember()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceService.UpdateCurrentWorkspaceMemberAsyncHandler = (_, member, _) => Task.FromResult<WorkspaceMember?>(new WorkspaceMember
        {
            UserId = member.UserId,
            WorkspaceId = member.WorkspaceId,
            Role = member.Role,
            JoinedAtUtc = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc),
        });

        using var client = factory.CreateClient(userId: 37);

        var response = await client.PutAsJsonAsync("/api/workspace/users/77", new UpdateWorkspaceMemberRequest
        {
            Role = "Manager",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<WorkspaceMemberResponse>();
        Assert.Equal(77, body.UserId);
        Assert.Equal("Manager", body.Role);
    }

    [Fact]
    public async Task GetWorkspaceThumbnail_ReturnsImage_WhenThumbnailExists()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.WorkspaceThumbnailService.GetCurrentWorkspaceThumbnailAsyncHandler = _ =>
            Task.FromResult<WorkspaceThumbnailContent?>(new WorkspaceThumbnailContent("image/png", [1, 2, 3]));

        using var client = factory.CreateClient(userId: 38);

        var response = await client.GetAsync("/api/workspace/thumbnail");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 1, 2, 3 }, bytes);
    }

    [Fact]
    public async Task GetWorkspaceThumbnail_ReturnsNotFound_WhenThumbnailDoesNotExist()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 38);

        var response = await client.GetAsync("/api/workspace/thumbnail");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutWorkspaceThumbnail_ReturnsNoContent_WhenUploadSucceeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 39);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([9, 8, 7])
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") },
        }, "file", "thumb.png");

        var response = await client.PutAsync("/api/workspace/thumbnail", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutWorkspaceThumbnail_ReturnsBadRequest_WhenFileIsEmpty()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 39);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([]), "file", "empty.png");

        var response = await client.PutAsync("/api/workspace/thumbnail", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_thumbnail", body.Error);
    }

    [Fact]
    public async Task DeleteWorkspaceThumbnail_ReturnsNoContent()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 40);

        var response = await client.DeleteAsync("/api/workspace/thumbnail");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
