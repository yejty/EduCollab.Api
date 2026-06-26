using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceCreationRequestService
    {
        Task<WorkspaceCreationRequest> SubmitRequestAsync(string name, string? description, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> GetCurrentUserLatestRequestAsync(CancellationToken cancellationToken);

        Task<List<WorkspaceCreationRequest>> GetRequestsAsync(WorkspaceCreationRequestStatus? status, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> ApproveRequestAsync(long requestId, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> DenyRequestAsync(long requestId, string? reason, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> ApproveRequestByReviewTokenAsync(long requestId, string reviewToken, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> DenyRequestByReviewTokenAsync(long requestId, string reviewToken, CancellationToken cancellationToken);
    }
}
