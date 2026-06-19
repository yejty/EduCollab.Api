using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Notifications;
using EduCollab.Application.Services.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Workspaces
{
    public sealed class WorkspaceCreationRequestService : IWorkspaceCreationRequestService
    {
        private readonly IWorkspaceCreationRequestRepository _creationRequestRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IPlatformAdminAuthorization _platformAdminAuthorization;
        private readonly INotificationService _notificationService;
        private readonly IOptions<PlatformAdminOptions> _platformAdminOptions;
        private readonly IOptions<WorkspaceCreationApprovalSettings> _approvalSettings;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<WorkspaceCreationRequestService> _logger;

        public WorkspaceCreationRequestService(
            IWorkspaceCreationRequestRepository creationRequestRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser,
            IPlatformAdminAuthorization platformAdminAuthorization,
            INotificationService notificationService,
            IOptions<PlatformAdminOptions> platformAdminOptions,
            IOptions<WorkspaceCreationApprovalSettings> approvalSettings,
            IHostEnvironment hostEnvironment,
            ILogger<WorkspaceCreationRequestService> logger)
        {
            _creationRequestRepository = creationRequestRepository;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _platformAdminAuthorization = platformAdminAuthorization;
            _notificationService = notificationService;
            _platformAdminOptions = platformAdminOptions;
            _approvalSettings = approvalSettings;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private static string RequireTrimmedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            return name.Trim();
        }

        public async Task<WorkspaceCreationRequest> SubmitRequestAsync(string name, string? description, CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            var normalizedName = RequireTrimmedName(name);

            if (await _workspaceRepository.IsUserInAnyWorkspaceAsync(userId, cancellationToken))
                throw new ArgumentException("You already belong to a workspace.");

            var existingPending = await _creationRequestRepository.GetLatestRequestForUserAsync(userId, cancellationToken);
            if (existingPending?.Status == WorkspaceCreationRequestStatus.Pending)
                throw new ArgumentException("You already have a pending workspace creation request.");

            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Authenticated user was not found.");

            var now = DateTime.UtcNow;
            var request = new WorkspaceCreationRequest
            {
                RequestedByUserId = userId,
                Name = normalizedName,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                Status = WorkspaceCreationRequestStatus.Pending,
                CreatedAtUtc = now,
            };

            request.Id = await _creationRequestRepository.InsertRequestAsync(request, cancellationToken);

            var adminEmail = _platformAdminOptions.Value.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                var reviewBaseUrl = _approvalSettings.Value.AdminReviewUrlBase?.Trim().TrimEnd('/') ?? string.Empty;
                var reviewHours = _approvalSettings.Value.AdminReviewTokenExpirationHours;
                var reviewExpiresAt = DateTimeOffset.UtcNow.AddHours(reviewHours);
                string? approveUrl = null;
                string? denyUrl = null;

                if (!string.IsNullOrEmpty(reviewBaseUrl))
                {
                    var approveToken = RefreshTokenGenerator.Create();
                    var denyToken = RefreshTokenGenerator.Create();

                    await _creationRequestRepository.InsertAdminReviewTokensAsync(
                        request.Id,
                        RefreshTokenGenerator.HashPlaintext(approveToken),
                        RefreshTokenGenerator.HashPlaintext(denyToken),
                        reviewExpiresAt,
                        DateTimeOffset.UtcNow,
                        cancellationToken);

                    approveUrl = $"{reviewBaseUrl}/{Uri.EscapeDataString(approveToken)}/approve";
                    denyUrl = $"{reviewBaseUrl}/{Uri.EscapeDataString(denyToken)}/deny";
                }

                var mail = EduCollabEmailTemplates.WorkspaceCreationRequestAdminNotification(
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    request.Name,
                    request.Description,
                    approveUrl,
                    denyUrl);

                await _notificationService.SendAsync(
                    NotificationMessage.Create(
                        adminEmail,
                        NotificationType.WorkspaceCreationRequestSubmitted,
                        mail,
                        actions: approveUrl is not null && denyUrl is not null
                            ? new[]
                            {
                                new NotificationAction("Approve request", approveUrl),
                                new NotificationAction("Deny request", denyUrl, NotificationActionStyle.Danger),
                            }
                            : null,
                        metadata: new Dictionary<string, string>
                        {
                            ["requestId"] = request.Id.ToString(),
                            ["requesterUserId"] = userId.ToString(),
                            ["workspaceName"] = request.Name,
                        }),
                    cancellationToken);
            }

            return request;
        }

        public async Task<WorkspaceCreationRequest?> GetCurrentUserLatestRequestAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            return await _creationRequestRepository.GetLatestRequestForUserAsync(userId, cancellationToken);
        }

        public async Task<List<WorkspaceCreationRequest>> GetRequestsAsync(
            WorkspaceCreationRequestStatus? status,
            CancellationToken cancellationToken)
        {
            await _platformAdminAuthorization.EnsureCurrentUserIsPlatformAdminAsync(cancellationToken);
            return await _creationRequestRepository.GetRequestsByStatusAsync(status, cancellationToken);
        }

        public async Task<WorkspaceCreationRequest?> ApproveRequestAsync(long requestId, CancellationToken cancellationToken)
        {
            await _platformAdminAuthorization.EnsureCurrentUserIsPlatformAdminAsync(cancellationToken);
            var reviewerUserId = RequireCurrentUserId();
            return await ApproveRequestInternalAsync(requestId, reviewerUserId, cancellationToken);
        }

        public async Task<WorkspaceCreationRequest?> DenyRequestAsync(long requestId, string? reason, CancellationToken cancellationToken)
        {
            await _platformAdminAuthorization.EnsureCurrentUserIsPlatformAdminAsync(cancellationToken);
            var reviewerUserId = RequireCurrentUserId();
            return await DenyRequestInternalAsync(requestId, reviewerUserId, reason, cancellationToken);
        }

        public async Task<WorkspaceCreationRequest?> ApproveRequestByReviewTokenAsync(string reviewToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(reviewToken))
                throw new ArgumentException("Review token is required.", nameof(reviewToken));

            var reviewerUserId = await ResolvePlatformAdminUserIdAsync(cancellationToken);
            var requestId = await _creationRequestRepository.ConsumeAdminReviewTokenAsync(
                RefreshTokenGenerator.HashPlaintext(reviewToken.Trim()),
                WorkspaceCreationAdminReviewAction.Approve,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (requestId is null)
                return null;

            return await ApproveRequestInternalAsync(requestId.Value, reviewerUserId, cancellationToken);
        }

        public async Task<WorkspaceCreationRequest?> DenyRequestByReviewTokenAsync(string reviewToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(reviewToken))
                throw new ArgumentException("Review token is required.", nameof(reviewToken));

            var reviewerUserId = await ResolvePlatformAdminUserIdAsync(cancellationToken);
            var requestId = await _creationRequestRepository.ConsumeAdminReviewTokenAsync(
                RefreshTokenGenerator.HashPlaintext(reviewToken.Trim()),
                WorkspaceCreationAdminReviewAction.Deny,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (requestId is null)
                return null;

            return await DenyRequestInternalAsync(
                requestId.Value,
                reviewerUserId,
                "Your workspace creation request was denied by the platform administrator.",
                cancellationToken);
        }

        private async Task<int> ResolvePlatformAdminUserIdAsync(CancellationToken cancellationToken)
        {
            var email = _platformAdminOptions.Value.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Platform admin email is not configured.");

            var credential = await _userRepository.GetCredentialByEmailAsync(email, cancellationToken);
            if (credential is null || !await _userRepository.IsPlatformAdminAsync(credential.Id, cancellationToken))
                throw new InvalidOperationException("Platform admin user was not found.");

            return credential.Id;
        }

        private async Task<WorkspaceCreationRequest?> ApproveRequestInternalAsync(
            long requestId,
            int reviewerUserId,
            CancellationToken cancellationToken)
        {
            if (requestId <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestId));

            var existing = await _creationRequestRepository.GetRequestByIdAsync(requestId, cancellationToken);
            if (existing is null)
                return null;

            if (existing.Status != WorkspaceCreationRequestStatus.Pending)
                throw new ArgumentException("Only pending workspace creation requests can be approved.");

            var requester = await _userRepository.GetUserByIdAsync(existing.RequestedByUserId, cancellationToken);
            if (requester is null)
                throw new InvalidOperationException("Requesting user was not found.");

            var now = DateTimeOffset.UtcNow;
            var hours = _approvalSettings.Value.TokenExpirationHours;
            var expiresAt = now.AddHours(hours);
            var plainToken = RefreshTokenGenerator.Create();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(plainToken);

            var approved = await _creationRequestRepository.ApproveRequestAsync(
                requestId,
                reviewerUserId,
                tokenHash,
                expiresAt,
                now,
                cancellationToken);

            if (approved is null)
                return null;

            await SendApprovedEmailAsync(requester, approved, plainToken, hours, requestId, cancellationToken);
            return approved;
        }

        private async Task<WorkspaceCreationRequest?> DenyRequestInternalAsync(
            long requestId,
            int reviewerUserId,
            string? reason,
            CancellationToken cancellationToken)
        {
            if (requestId <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestId));

            var existing = await _creationRequestRepository.GetRequestByIdAsync(requestId, cancellationToken);
            if (existing is null)
                return null;

            if (existing.Status != WorkspaceCreationRequestStatus.Pending)
                throw new ArgumentException("Only pending workspace creation requests can be denied.");

            var requester = await _userRepository.GetUserByIdAsync(existing.RequestedByUserId, cancellationToken);
            if (requester is null)
                throw new InvalidOperationException("Requesting user was not found.");

            var denied = await _creationRequestRepository.DenyRequestAsync(
                requestId,
                reviewerUserId,
                string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (denied is null)
                return null;

            var mail = EduCollabEmailTemplates.WorkspaceCreationDenied(
                requester.FirstName,
                denied.Name,
                denied.DenialReason);

            await _notificationService.SendAsync(
                NotificationMessage.Create(
                    requester.Email,
                    NotificationType.WorkspaceCreationDenied,
                    mail,
                    metadata: new Dictionary<string, string>
                    {
                        ["requestId"] = requestId.ToString(),
                        ["workspaceName"] = denied.Name,
                    }),
                cancellationToken);

            return denied;
        }

        private async Task SendApprovedEmailAsync(
            User requester,
            WorkspaceCreationRequest approved,
            string plainToken,
            int hours,
            long requestId,
            CancellationToken cancellationToken)
        {
            var baseUrl = _approvalSettings.Value.FrontendCreateUrl?.Trim().TrimEnd('/') ?? string.Empty;
            string? createUrl = null;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                createUrl = $"{baseUrl}?token={Uri.EscapeDataString(plainToken)}";
            }

            var mail = EduCollabEmailTemplates.WorkspaceCreationApproved(
                requester.FirstName,
                approved.Name,
                createUrl,
                plainToken,
                hours);

            await _notificationService.SendAsync(
                NotificationMessage.Create(
                    requester.Email,
                    NotificationType.WorkspaceCreationApproved,
                    mail,
                    actions: string.IsNullOrWhiteSpace(createUrl)
                        ? null
                        : new[] { new NotificationAction("Create workspace", createUrl) },
                    metadata: new Dictionary<string, string>
                    {
                        ["requestId"] = requestId.ToString(),
                        ["workspaceName"] = approved.Name,
                    }),
                cancellationToken);

            if (_hostEnvironment.IsDevelopment() && _approvalSettings.Value.LogPlaintextTokenInDevelopment)
            {
                _logger.LogInformation(
                    "Workspace creation approval token for request {RequestId}: {Token}",
                    requestId,
                    plainToken);
            }
        }
    }
}
