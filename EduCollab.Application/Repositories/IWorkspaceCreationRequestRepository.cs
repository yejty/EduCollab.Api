using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IWorkspaceCreationRequestRepository
    {
        Task<long> InsertRequestAsync(WorkspaceCreationRequest request, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> GetRequestByIdAsync(long requestId, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> GetLatestRequestForUserAsync(int userId, CancellationToken cancellationToken);

        Task<List<WorkspaceCreationRequest>> GetRequestsByStatusAsync(WorkspaceCreationRequestStatus? status, CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> ApproveRequestAsync(
            long requestId,
            int reviewerUserId,
            string tokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset reviewedAtUtc,
            CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> DenyRequestAsync(
            long requestId,
            int reviewerUserId,
            string? denialReason,
            DateTimeOffset reviewedAtUtc,
            CancellationToken cancellationToken);

        Task<WorkspaceCreationRequest?> ConsumeApprovalTokenAsync(
            int userId,
            string tokenHashSha256Hex,
            string workspaceName,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken);

        Task InsertAdminReviewTokensAsync(
            long requestId,
            string approveTokenHashSha256Hex,
            string denyTokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken);

        Task<long?> ConsumeAdminReviewTokenAsync(
            string tokenHashSha256Hex,
            WorkspaceCreationAdminReviewAction action,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken);

        Task InvalidateAdminReviewTokensForRequestAsync(long requestId, CancellationToken cancellationToken);
    }
}
