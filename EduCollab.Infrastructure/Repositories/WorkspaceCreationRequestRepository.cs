using Dapper;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Infrastructure.Database;
using System.Data.Common;

namespace EduCollab.Infrastructure.Repositories
{
    public class WorkspaceCreationRequestRepository : IWorkspaceCreationRequestRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public WorkspaceCreationRequestRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<long> InsertRequestAsync(WorkspaceCreationRequest request, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            return await connection.QuerySingleAsync<long>(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceCreationRequests (
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc)
                    VALUES (
                        @RequestedByUserId,
                        @Name,
                        @Description,
                        @Status,
                        @CreatedAtUtc)
                    RETURNING Id;
                    """,
                    new
                    {
                        request.RequestedByUserId,
                        request.Name,
                        request.Description,
                        Status = request.Status.ToPersistedString(),
                        request.CreatedAtUtc,
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<WorkspaceCreationRequest?> GetRequestByIdAsync(long requestId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var row = await connection.QuerySingleOrDefaultAsync<WorkspaceCreationRequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc,
                        ReviewedAtUtc,
                        ReviewedByUserId,
                        DenialReason
                    FROM WorkspaceCreationRequests
                    WHERE Id = @RequestId
                    LIMIT 1;
                    """,
                    new { RequestId = requestId },
                    cancellationToken: cancellationToken));

            return row is null ? null : MapRow(row);
        }

        public async Task<WorkspaceCreationRequest?> GetLatestRequestForUserAsync(int userId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var row = await connection.QuerySingleOrDefaultAsync<WorkspaceCreationRequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc,
                        ReviewedAtUtc,
                        ReviewedByUserId,
                        DenialReason
                    FROM WorkspaceCreationRequests
                    WHERE RequestedByUserId = @UserId
                    ORDER BY CreatedAtUtc DESC, Id DESC
                    LIMIT 1;
                    """,
                    new { UserId = userId },
                    cancellationToken: cancellationToken));

            return row is null ? null : MapRow(row);
        }

        public async Task<List<WorkspaceCreationRequest>> GetRequestsByStatusAsync(
            WorkspaceCreationRequestStatus? status,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var rows = await connection.QueryAsync<WorkspaceCreationRequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc,
                        ReviewedAtUtc,
                        ReviewedByUserId,
                        DenialReason
                    FROM WorkspaceCreationRequests
                    WHERE @Status IS NULL OR Status = @Status
                    ORDER BY CreatedAtUtc DESC, Id DESC;
                    """,
                    new { Status = status?.ToPersistedString() },
                    cancellationToken: cancellationToken));

            return rows.Select(MapRow).AsList();
        }

        public async Task<WorkspaceCreationRequest?> ApproveRequestAsync(
            long requestId,
            int reviewerUserId,
            string tokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset reviewedAtUtc,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
                throw new InvalidOperationException("Database connection must support transactions.");

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var locked = await connection.QuerySingleOrDefaultAsync<WorkspaceCreationRequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc,
                        ReviewedAtUtc,
                        ReviewedByUserId,
                        DenialReason
                    FROM WorkspaceCreationRequests
                    WHERE Id = @RequestId
                      AND Status = @PendingStatus
                    FOR UPDATE;
                    """,
                    new
                    {
                        RequestId = requestId,
                        PendingStatus = WorkspaceCreationRequestStatus.Pending.ToPersistedString(),
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (locked is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceCreationRequests
                    SET Status = @ApprovedStatus,
                        ReviewedAtUtc = @ReviewedAtUtc,
                        ReviewedByUserId = @ReviewedByUserId
                    WHERE Id = @RequestId;
                    """,
                    new
                    {
                        RequestId = requestId,
                        ApprovedStatus = WorkspaceCreationRequestStatus.Approved.ToPersistedString(),
                        ReviewedAtUtc = reviewedAtUtc.UtcDateTime,
                        ReviewedByUserId = reviewerUserId,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceCreationApprovalTokens (RequestId, TokenHash, ExpiresAt, CreatedAt)
                    VALUES (@RequestId, @TokenHash, @ExpiresAt, @CreatedAt);
                    """,
                    new
                    {
                        RequestId = requestId,
                        TokenHash = tokenHashSha256Hex,
                        ExpiresAt = expiresAtUtc,
                        CreatedAt = reviewedAtUtc,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            locked.Status = WorkspaceCreationRequestStatus.Approved.ToPersistedString();
            locked.ReviewedAtUtc = reviewedAtUtc.UtcDateTime;
            locked.ReviewedByUserId = reviewerUserId;

            await tx.CommitAsync(cancellationToken);

            await InvalidateAdminReviewTokensForRequestAsync(requestId, cancellationToken);

            return MapRow(locked);
        }

        public async Task<WorkspaceCreationRequest?> DenyRequestAsync(
            long requestId,
            int reviewerUserId,
            string? denialReason,
            DateTimeOffset reviewedAtUtc,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            var row = await connection.QuerySingleOrDefaultAsync<WorkspaceCreationRequestRow>(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceCreationRequests
                    SET Status = @DeniedStatus,
                        ReviewedAtUtc = @ReviewedAtUtc,
                        ReviewedByUserId = @ReviewedByUserId,
                        DenialReason = @DenialReason
                    WHERE Id = @RequestId
                      AND Status = @PendingStatus
                    RETURNING
                        Id,
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc,
                        ReviewedAtUtc,
                        ReviewedByUserId,
                        DenialReason;
                    """,
                    new
                    {
                        RequestId = requestId,
                        DeniedStatus = WorkspaceCreationRequestStatus.Denied.ToPersistedString(),
                        PendingStatus = WorkspaceCreationRequestStatus.Pending.ToPersistedString(),
                        ReviewedAtUtc = reviewedAtUtc.UtcDateTime,
                        ReviewedByUserId = reviewerUserId,
                        DenialReason = denialReason,
                    },
                    cancellationToken: cancellationToken));

            if (row is not null)
            {
                await InvalidateAdminReviewTokensForRequestAsync(requestId, cancellationToken);
            }

            return row is null ? null : MapRow(row);
        }

        public async Task<WorkspaceCreationRequest?> ConsumeApprovalTokenAsync(
            int userId,
            string tokenHashSha256Hex,
            string workspaceName,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
                throw new InvalidOperationException("Database connection must support transactions.");

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var tokenRow = await connection.QuerySingleOrDefaultAsync<LockedApprovalTokenRow>(
                new CommandDefinition(
                    """
                    SELECT t.Id, t.RequestId, r.Name
                    FROM WorkspaceCreationApprovalTokens t
                    INNER JOIN WorkspaceCreationRequests r ON r.Id = t.RequestId
                    WHERE t.TokenHash = @TokenHash
                      AND t.UsedAt IS NULL
                      AND t.ExpiresAt > @Now
                      AND r.Status = @ApprovedStatus
                      AND r.RequestedByUserId = @UserId
                    ORDER BY t.Id DESC
                    LIMIT 1
                    FOR UPDATE OF t;
                    """,
                    new
                    {
                        TokenHash = tokenHashSha256Hex,
                        Now = utcNow,
                        ApprovedStatus = WorkspaceCreationRequestStatus.Approved.ToPersistedString(),
                        UserId = userId,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (tokenRow is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(tokenRow.Name.Trim(), workspaceName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceCreationApprovalTokens
                    SET UsedAt = @Now
                    WHERE Id = @Id;
                    """,
                    new { tokenRow.Id, Now = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            var requestRow = await connection.QuerySingleAsync<WorkspaceCreationRequestRow>(
                new CommandDefinition(
                    """
                    SELECT
                        Id,
                        RequestedByUserId,
                        Name,
                        Description,
                        Status,
                        CreatedAtUtc,
                        ReviewedAtUtc,
                        ReviewedByUserId,
                        DenialReason
                    FROM WorkspaceCreationRequests
                    WHERE Id = @RequestId;
                    """,
                    new { RequestId = tokenRow.RequestId },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return MapRow(requestRow);
        }

        public async Task InsertAdminReviewTokensAsync(
            long requestId,
            string approveTokenHashSha256Hex,
            string denyTokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO WorkspaceCreationAdminReviewTokens (RequestId, TokenHash, Action, ExpiresAt, CreatedAt)
                    VALUES
                        (@RequestId, @ApproveTokenHash, @ApproveAction, @ExpiresAt, @CreatedAt),
                        (@RequestId, @DenyTokenHash, @DenyAction, @ExpiresAt, @CreatedAt);
                    """,
                    new
                    {
                        RequestId = requestId,
                        ApproveTokenHash = approveTokenHashSha256Hex,
                        DenyTokenHash = denyTokenHashSha256Hex,
                        ApproveAction = WorkspaceCreationAdminReviewAction.Approve.ToPersistedString(),
                        DenyAction = WorkspaceCreationAdminReviewAction.Deny.ToPersistedString(),
                        ExpiresAt = expiresAtUtc,
                        CreatedAt = createdAtUtc,
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<long?> ConsumeAdminReviewTokenAsync(
            string tokenHashSha256Hex,
            WorkspaceCreationAdminReviewAction action,
            long expectedRequestId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            if (connection is not DbConnection dbConnection)
                throw new InvalidOperationException("Database connection must support transactions.");

            await using var tx = await dbConnection.BeginTransactionAsync(cancellationToken);

            var tokenRow = await connection.QuerySingleOrDefaultAsync<AdminReviewTokenRow>(
                new CommandDefinition(
                    """
                    SELECT t.Id, t.RequestId, t.Action
                    FROM WorkspaceCreationAdminReviewTokens t
                    INNER JOIN WorkspaceCreationRequests r ON r.Id = t.RequestId
                    WHERE t.TokenHash = @TokenHash
                      AND t.RequestId = @ExpectedRequestId
                      AND t.UsedAt IS NULL
                      AND t.ExpiresAt > @Now
                      AND r.Status = @PendingStatus
                    LIMIT 1
                    FOR UPDATE OF t;
                    """,
                    new
                    {
                        TokenHash = tokenHashSha256Hex,
                        ExpectedRequestId = expectedRequestId,
                        Now = utcNow,
                        PendingStatus = WorkspaceCreationRequestStatus.Pending.ToPersistedString(),
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            if (tokenRow is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(tokenRow.Action, action.ToPersistedString(), StringComparison.Ordinal))
            {
                await tx.RollbackAsync(cancellationToken);
                return null;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceCreationAdminReviewTokens
                    SET UsedAt = @Now
                    WHERE RequestId = @RequestId
                      AND UsedAt IS NULL;
                    """,
                    new { tokenRow.RequestId, Now = utcNow },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return tokenRow.RequestId;
        }

        public async Task InvalidateAdminReviewTokensForRequestAsync(long requestId, CancellationToken cancellationToken)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE WorkspaceCreationAdminReviewTokens
                    SET UsedAt = COALESCE(UsedAt, NOW())
                    WHERE RequestId = @RequestId
                      AND UsedAt IS NULL;
                    """,
                    new { RequestId = requestId },
                    cancellationToken: cancellationToken));
        }

        private static WorkspaceCreationRequest MapRow(WorkspaceCreationRequestRow row) =>
            new()
            {
                Id = row.Id,
                RequestedByUserId = row.RequestedByUserId,
                Name = row.Name,
                Description = row.Description,
                Status = WorkspaceCreationRequestStatusExtensions.FromPersisted(row.Status),
                CreatedAtUtc = row.CreatedAtUtc,
                ReviewedAtUtc = row.ReviewedAtUtc,
                ReviewedByUserId = row.ReviewedByUserId,
                DenialReason = row.DenialReason,
            };

        private sealed class WorkspaceCreationRequestRow
        {
            public long Id { get; set; }
            public int RequestedByUserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
            public DateTime? ReviewedAtUtc { get; set; }
            public int? ReviewedByUserId { get; set; }
            public string? DenialReason { get; set; }
        }

        private sealed class LockedApprovalTokenRow
        {
            public long Id { get; set; }
            public long RequestId { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private sealed class AdminReviewTokenRow
        {
            public long Id { get; set; }
            public long RequestId { get; set; }
            public string Action { get; set; } = string.Empty;
        }
    }
}
