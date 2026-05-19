using EduCollab.Application.Models.Users;
using EduCollab.Application.Models.Workspaces;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeWorkspaceService : IWorkspaceService
{
    public Func<int, User, string, string, CancellationToken, Task<bool>>? CreateUserInWorkspaceAsyncHandler { get; set; }
    public Func<int, string, CancellationToken, Task>? InviteUserToWorkspaceAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<Workspace?>>? GetWorkspaceAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Workspace>>>? GetWorkspacesAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<WorkspaceMember>>>? GetWorkspaceMembersAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task<WorkspaceMember?>>? GetWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<WorkspaceMember?>>? GetCurrentUserWorkspaceMemberAsyncHandler { get; set; }
    public Func<Workspace, CancellationToken, Task<bool>>? CreateWorkspaceAsyncHandler { get; set; }
    public Func<Workspace, CancellationToken, Task<bool>>? UpdateWorkspaceAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? DeleteWorkspaceAsyncHandler { get; set; }
    public Func<int, int, CancellationToken, Task>? RemoveWorkspaceMemberAsyncHandler { get; set; }
    public Func<int, int, WorkspaceMember, CancellationToken, Task<WorkspaceMember?>>? UpdateWorkspaceMemberAsyncHandler { get; set; }

    public Task<bool> CreateUserInWorkspaceAsync(int workspaceId, User user, string password, string invitationToken, CancellationToken cancellationToken) =>
        CreateUserInWorkspaceAsyncHandler?.Invoke(workspaceId, user, password, invitationToken, cancellationToken)
        ?? Task.FromResult(true);

    public Task InviteUserToWorkspaceAsync(int workspaceId, string email, CancellationToken cancellationToken) =>
        InviteUserToWorkspaceAsyncHandler?.Invoke(workspaceId, email, cancellationToken) ?? Task.CompletedTask;

    public Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken) =>
        GetWorkspaceAsyncHandler?.Invoke(id, cancellationToken) ?? Task.FromResult<Workspace?>(null);

    public Task<List<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken) =>
        GetWorkspacesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Workspace>());

    public Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int id, CancellationToken cancellationToken) =>
        GetWorkspaceMembersAsyncHandler?.Invoke(id, cancellationToken) ?? Task.FromResult(new List<WorkspaceMember>());

    public Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
        GetWorkspaceMemberAsyncHandler?.Invoke(workspaceId, userId, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(int workspaceId, CancellationToken cancellationToken) =>
        GetCurrentUserWorkspaceMemberAsyncHandler?.Invoke(workspaceId, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(null);

    public Task<bool> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken) =>
        CreateWorkspaceAsyncHandler?.Invoke(workspace, cancellationToken) ?? Task.FromResult(true);

    public Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken) =>
        UpdateWorkspaceAsyncHandler?.Invoke(workspace, cancellationToken) ?? Task.FromResult(true);

    public Task<bool> DeleteWorkspaceAsync(int workspaceId, CancellationToken cancellationToken) =>
        DeleteWorkspaceAsyncHandler?.Invoke(workspaceId, cancellationToken) ?? Task.FromResult(true);

    public Task RemoveWorkspaceMemberAsync(int workspaceId, int targetUserId, CancellationToken cancellationToken) =>
        RemoveWorkspaceMemberAsyncHandler?.Invoke(workspaceId, targetUserId, cancellationToken) ?? Task.CompletedTask;

    public Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken) =>
        UpdateWorkspaceMemberAsyncHandler?.Invoke(id, userId, member, cancellationToken) ?? Task.FromResult<WorkspaceMember?>(member);
}
