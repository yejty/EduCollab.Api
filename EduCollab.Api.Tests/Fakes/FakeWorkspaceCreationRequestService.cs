using EduCollab.Application.Models;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeWorkspaceCreationRequestService : IWorkspaceCreationRequestService
{
    public Func<string, string?, CancellationToken, Task<WorkspaceCreationRequest>>? SubmitRequestAsyncHandler { get; set; }
    public Func<CancellationToken, Task<WorkspaceCreationRequest?>>? GetCurrentUserLatestRequestAsyncHandler { get; set; }
    public Func<WorkspaceCreationRequestStatus?, CancellationToken, Task<List<WorkspaceCreationRequest>>>? GetRequestsAsyncHandler { get; set; }
    public Func<long, CancellationToken, Task<WorkspaceCreationRequest?>>? ApproveRequestAsyncHandler { get; set; }
    public Func<long, string?, CancellationToken, Task<WorkspaceCreationRequest?>>? DenyRequestAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task<WorkspaceCreationRequest?>>? ApproveRequestByReviewTokenAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task<WorkspaceCreationRequest?>>? DenyRequestByReviewTokenAsyncHandler { get; set; }

    public Task<WorkspaceCreationRequest> SubmitRequestAsync(string name, string? description, CancellationToken cancellationToken) =>
        SubmitRequestAsyncHandler?.Invoke(name, description, cancellationToken)
        ?? Task.FromResult(new WorkspaceCreationRequest
        {
            Id = 1,
            Name = name,
            Description = description,
            Status = WorkspaceCreationRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });

    public Task<WorkspaceCreationRequest?> GetCurrentUserLatestRequestAsync(CancellationToken cancellationToken) =>
        GetCurrentUserLatestRequestAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult<WorkspaceCreationRequest?>(null);

    public Task<List<WorkspaceCreationRequest>> GetRequestsAsync(WorkspaceCreationRequestStatus? status, CancellationToken cancellationToken) =>
        GetRequestsAsyncHandler?.Invoke(status, cancellationToken) ?? Task.FromResult(new List<WorkspaceCreationRequest>());

    public Task<WorkspaceCreationRequest?> ApproveRequestAsync(long requestId, CancellationToken cancellationToken) =>
        ApproveRequestAsyncHandler?.Invoke(requestId, cancellationToken) ?? Task.FromResult<WorkspaceCreationRequest?>(null);

    public Task<WorkspaceCreationRequest?> DenyRequestAsync(long requestId, string? reason, CancellationToken cancellationToken) =>
        DenyRequestAsyncHandler?.Invoke(requestId, reason, cancellationToken) ?? Task.FromResult<WorkspaceCreationRequest?>(null);

    public Task<WorkspaceCreationRequest?> ApproveRequestByReviewTokenAsync(string reviewToken, CancellationToken cancellationToken) =>
        ApproveRequestByReviewTokenAsyncHandler?.Invoke(reviewToken, cancellationToken) ?? Task.FromResult<WorkspaceCreationRequest?>(null);

    public Task<WorkspaceCreationRequest?> DenyRequestByReviewTokenAsync(string reviewToken, CancellationToken cancellationToken) =>
        DenyRequestByReviewTokenAsyncHandler?.Invoke(reviewToken, cancellationToken) ?? Task.FromResult<WorkspaceCreationRequest?>(null);
}
