using EduCollab.Application.Models;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeWorkspaceService : IWorkspaceService
{
    public Func<int, User, string, string, CancellationToken, Task<bool>>? CreateUserInWorkspaceAsyncHandler { get; set; }
    public Func<User, string, string, CancellationToken, Task<bool>>? CreateUserFromInvitationAsyncHandler { get; set; }
    public Func<int, string, WorkspaceRole, CancellationToken, Task>? InviteUserToWorkspaceAsyncHandler { get; set; }
    public Func<string, WorkspaceRole, CancellationToken, Task>? InviteUserToCurrentWorkspaceAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task<WorkspaceMember?>>? JoinWorkspaceFromInvitationAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<Workspace?>>? GetWorkspaceAsyncHandler { get; set; }
    public Func<CancellationToken, Task<Workspace?>>? GetCurrentWorkspaceAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Workspace>>>? GetWorkspacesAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<WorkspaceMember>>>? GetWorkspaceMembersAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<WorkspaceMember>>>? GetCurrentWorkspaceMembersAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<WorkspaceMember?>>? GetWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<WorkspaceMember?>>? GetCurrentWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<WorkspaceMember?>>? GetCurrentUserWorkspaceMemberAsyncHandler { get; set; }
    public Func<CancellationToken, Task<WorkspaceMember?>>? GetCurrentUserWorkspaceMemberForCurrentWorkspaceAsyncHandler { get; set; }
    public Func<Workspace, string, CancellationToken, Task<bool>>? CreateWorkspaceAsyncHandler { get; set; }
    public Func<Workspace, CancellationToken, Task<bool>>? UpdateWorkspaceAsyncHandler { get; set; }
    public Func<Workspace, CancellationToken, Task<bool>>? UpdateCurrentWorkspaceAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? DeleteWorkspaceAsyncHandler { get; set; }
    public Func<CancellationToken, Task<bool>>? DeleteCurrentWorkspaceAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task>? RemoveWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task>? RemoveCurrentWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, int, WorkspaceMember, CancellationToken, Task<WorkspaceMember?>>? UpdateWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, WorkspaceMember, CancellationToken, Task<WorkspaceMember?>>? UpdateCurrentWorkspaceMemberAsyncHandler { get; set; }

    public Task<bool> CreateUserInWorkspaceAsync(int workspaceId, User user, string password, string invitationToken, CancellationToken cancellationToken) =>
        CreateUserInWorkspaceAsyncHandler?.Invoke(workspaceId, user, password, invitationToken, cancellationToken)
        ?? Task.FromResult(true);

    public Task<bool> CreateUserFromInvitationAsync(User user, string password, string invitationToken, CancellationToken cancellationToken) =>
        CreateUserFromInvitationAsyncHandler?.Invoke(user, password, invitationToken, cancellationToken)
        ?? Task.FromResult(true);

    public Task InviteUserToWorkspaceAsync(int workspaceId, string email, WorkspaceRole role, CancellationToken cancellationToken) =>
        InviteUserToWorkspaceAsyncHandler?.Invoke(workspaceId, email, role, cancellationToken) ?? Task.CompletedTask;

    public Task InviteUserToCurrentWorkspaceAsync(string email, WorkspaceRole role, CancellationToken cancellationToken) =>
        InviteUserToCurrentWorkspaceAsyncHandler?.Invoke(email, role, cancellationToken) ?? Task.CompletedTask;

    public Task<WorkspaceMember?> JoinWorkspaceFromInvitationAsync(string invitationToken, CancellationToken cancellationToken) =>
        JoinWorkspaceFromInvitationAsyncHandler?.Invoke(invitationToken, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken) =>
        GetWorkspaceAsyncHandler?.Invoke(id, cancellationToken) ?? Task.FromResult<Workspace?>(null);

    public Task<Workspace?> GetCurrentWorkspaceAsync(CancellationToken cancellationToken) =>
        GetCurrentWorkspaceAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult<Workspace?>(null);

    public Task<List<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken) =>
        GetWorkspacesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Workspace>());

    public Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int id, CancellationToken cancellationToken) =>
        GetWorkspaceMembersAsyncHandler?.Invoke(id, cancellationToken) ?? Task.FromResult(new List<WorkspaceMember>());

    public Task<List<WorkspaceMember>> GetCurrentWorkspaceMembersAsync(CancellationToken cancellationToken) =>
        GetCurrentWorkspaceMembersAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<WorkspaceMember>());

    public Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
        GetWorkspaceMemberAsyncHandler?.Invoke(workspaceId, userId, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<WorkspaceMember?> GetCurrentWorkspaceMemberAsync(int userId, CancellationToken cancellationToken) =>
        GetCurrentWorkspaceMemberAsyncHandler?.Invoke(userId, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(int workspaceId, CancellationToken cancellationToken) =>
        GetCurrentUserWorkspaceMemberAsyncHandler?.Invoke(workspaceId, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(CancellationToken cancellationToken) =>
        GetCurrentUserWorkspaceMemberForCurrentWorkspaceAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<bool> CreateWorkspaceAsync(Workspace workspace, string approvalToken, CancellationToken cancellationToken) =>
        CreateWorkspaceAsyncHandler?.Invoke(workspace, approvalToken, cancellationToken) ?? Task.FromResult(true);

    public Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken) =>
        UpdateWorkspaceAsyncHandler?.Invoke(workspace, cancellationToken) ?? Task.FromResult(true);

    public Task<bool> UpdateCurrentWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken) =>
        UpdateCurrentWorkspaceAsyncHandler?.Invoke(workspace, cancellationToken) ?? Task.FromResult(true);

    public Task<bool> DeleteWorkspaceAsync(int workspaceId, CancellationToken cancellationToken) =>
        DeleteWorkspaceAsyncHandler?.Invoke(workspaceId, cancellationToken) ?? Task.FromResult(true);

    public Task<bool> DeleteCurrentWorkspaceAsync(CancellationToken cancellationToken) =>
        DeleteCurrentWorkspaceAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(true);

    public Task RemoveWorkspaceMemberAsync(int workspaceId, int targetUserId, CancellationToken cancellationToken) =>
        RemoveWorkspaceMemberAsyncHandler?.Invoke(workspaceId, targetUserId, cancellationToken) ?? Task.CompletedTask;

    public Task RemoveCurrentWorkspaceMemberAsync(int targetUserId, CancellationToken cancellationToken) =>
        RemoveCurrentWorkspaceMemberAsyncHandler?.Invoke(targetUserId, cancellationToken) ?? Task.CompletedTask;

    public Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken) =>
        UpdateWorkspaceMemberAsyncHandler?.Invoke(id, userId, member, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(member);

    public Task<WorkspaceMember?> UpdateCurrentWorkspaceMemberAsync(int userId, WorkspaceMember member, CancellationToken cancellationToken) =>
        UpdateCurrentWorkspaceMemberAsyncHandler?.Invoke(userId, member, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(member);
}
