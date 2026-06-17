using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Workspaces
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IOptions<WorkspaceInvitationSettings> _invitationSettings;
        private readonly INotificationService _notificationService;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(
            IUserRepository userRepository,
            IWorkspaceRepository workspaceRepository,
            ICurrentUser currentUser,
            IOptions<WorkspaceInvitationSettings> invitationSettings,
            INotificationService notificationService,
            IHostEnvironment hostEnvironment,
            ILogger<WorkspaceService> logger)
        {
            _userRepository = userRepository;
            _workspaceRepository = workspaceRepository;
            _currentUser = currentUser;
            _invitationSettings = invitationSettings;
            _notificationService = notificationService;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private async Task<int?> GetCurrentWorkspaceIdOrNullAsync(CancellationToken cancellationToken)
        {
            if (_currentUser.UserId is not int userId)
            {
                return null;
            }

            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
            return user?.WorkspaceId is int workspaceId && workspaceId > 0
                ? workspaceId
                : null;
        }

        private async Task<(int WorkspaceId, WorkspaceMember Membership)> RequireCurrentWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            var workspaceId = await GetCurrentWorkspaceIdOrNullAsync(cancellationToken);
            if (workspaceId is null)
            {
                throw new AccessDeniedException("You are not a member of any workspace.");
            }

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId.Value, userId, cancellationToken);
            if (membership is null)
            {
                throw new AccessDeniedException("You are not a member of this workspace.");
            }

            return (workspaceId.Value, membership);
        }

        public async Task<Workspace?> GetWorkspaceAsync(int id, CancellationToken cancellationToken)
        {
            return await _workspaceRepository.GetWorkspaceByIdAsync(id, cancellationToken);
        }

        public async Task<Workspace?> GetCurrentWorkspaceAsync(CancellationToken cancellationToken)
        {
            var workspaceId = await GetCurrentWorkspaceIdOrNullAsync(cancellationToken);
            if (workspaceId is null)
            {
                return null;
            }

            return await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId.Value, cancellationToken);
        }

        public Task<List<Workspace>> GetWorkspacesAsync(CancellationToken cancellationToken) =>
            _workspaceRepository.GetAllWorkspacesAsync(cancellationToken);

        public async Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int id, CancellationToken cancellationToken)
        {
            return await _workspaceRepository.GetWorkspaceMembersAsync(id, cancellationToken);
        }

        public async Task<List<WorkspaceMember>> GetCurrentWorkspaceMembersAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            return await _workspaceRepository.GetWorkspaceMembersAsync(workspaceId, cancellationToken);
        }

        public async Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<WorkspaceMember?> GetCurrentWorkspaceMemberAsync(int userId, CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            return await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(int workspaceId, CancellationToken cancellationToken)
        {
            if (_currentUser.UserId is not int userId)
            {
                return null;
            }

            return await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<WorkspaceMember?> GetCurrentUserWorkspaceMemberAsync(CancellationToken cancellationToken)
        {
            var workspaceId = await GetCurrentWorkspaceIdOrNullAsync(cancellationToken);
            if (workspaceId is null || _currentUser.UserId is not int userId)
            {
                return null;
            }

            return await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId.Value, userId, cancellationToken);
        }

        public async Task<bool> CreateUserInWorkspaceAsync(int workspaceId, User user, string password, string invitationToken, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));

            if (string.IsNullOrWhiteSpace(invitationToken))
                throw new ArgumentException($"'{nameof(invitationToken)}' cannot be null or empty.", nameof(invitationToken));

            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            var normalizedEmail = user.Email.Trim();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(invitationToken.Trim());
            var now = DateTimeOffset.UtcNow;

            var userId = await _workspaceRepository.AcceptWorkspaceInvitationAndRegisterUserAsync(
                workspaceId,
                tokenHash,
                normalizedEmail,
                user.FirstName.Trim(),
                user.LastName.Trim(),
                password,
                now,
                cancellationToken);

            if (userId is null)
                return false;

            user.Id = userId.Value;
            user.WorkspaceId = workspaceId;
            user.Email = normalizedEmail;
            return true;
        }

        public async Task<bool> CreateUserFromInvitationAsync(User user, string password, string invitationToken, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(invitationToken))
                throw new ArgumentException($"'{nameof(invitationToken)}' cannot be null or empty.", nameof(invitationToken));

            var tokenHash = RefreshTokenGenerator.HashPlaintext(invitationToken.Trim());
            var invitation = await _workspaceRepository.GetActiveWorkspaceInvitationAsync(
                tokenHash,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (invitation is null)
            {
                return false;
            }

            return await CreateUserInWorkspaceAsync(invitation.WorkspaceId, user, password, invitationToken, cancellationToken);
        }

        public async Task<WorkspaceMember?> JoinWorkspaceFromInvitationAsync(string invitationToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(invitationToken))
                throw new ArgumentException($"'{nameof(invitationToken)}' cannot be null or empty.", nameof(invitationToken));

            var userId = RequireCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Authenticated user was not found.");

            if (await _workspaceRepository.IsUserInAnyWorkspaceAsync(userId, cancellationToken))
                throw new ArgumentException("You already belong to a workspace.");

            var tokenHash = RefreshTokenGenerator.HashPlaintext(invitationToken.Trim());
            var invitation = await _workspaceRepository.GetActiveWorkspaceInvitationAsync(
                tokenHash,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (invitation is null)
                return null;

            if (!string.Equals(user.Email.Trim(), invitation.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new AccessDeniedException("This invitation was sent to a different email address.");

            return await _workspaceRepository.AcceptWorkspaceInvitationForExistingUserAsync(
                invitation.WorkspaceId,
                tokenHash,
                userId,
                user.Email.Trim(),
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        public async Task InviteUserToWorkspaceAsync(int workspaceId, string email, WorkspaceRole role, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            var inviterUserId = RequireCurrentUserId();

            var normalizedEmail = email.Trim();

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                throw new ArgumentException("Workspace not found.");

            var inviterMember = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, inviterUserId, cancellationToken);
            if (inviterMember is null)
                throw new UnauthorizedAccessException("You are not a member of this workspace.");

            if (!WorkspaceRolePermissions.CanInviteUsers(inviterMember.Role))
                throw new UnauthorizedAccessException("Only workspace owners and managers can send invitations.");

            EnsureInviterCanAssignRole(inviterMember.Role, role);

            var existingCred = await _userRepository.GetCredentialByEmailAsync(normalizedEmail, cancellationToken);
            if (existingCred is not null
                && await _workspaceRepository.IsUserInAnyWorkspaceAsync(existingCred.Id, cancellationToken))
            {
                throw new ArgumentException("This user already belongs to a workspace.");
            }

            var alreadyMember = await _workspaceRepository.IsEmailMemberOfWorkspaceAsync(workspaceId, normalizedEmail, cancellationToken);
            if (alreadyMember)
                throw new ArgumentException("This user is already a member of the workspace.");

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddHours(_invitationSettings.Value.TokenExpirationHours);

            await _workspaceRepository.RevokePendingWorkspaceInvitationsAsync(workspaceId, normalizedEmail, now, cancellationToken);

            var plainToken = RefreshTokenGenerator.Create();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(plainToken);

            await _workspaceRepository.InsertWorkspaceInvitationAsync(
                workspaceId,
                normalizedEmail,
                tokenHash,
                role,
                expiresAt,
                now,
                inviterUserId,
                cancellationToken);

            var hours = _invitationSettings.Value.TokenExpirationHours;
            var baseUrl = _invitationSettings.Value.FrontendAcceptUrl?.Trim().TrimEnd('/') ?? string.Empty;
            string? acceptUrl = null;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                acceptUrl =
                    $"{baseUrl}/{workspaceId}?token={Uri.EscapeDataString(plainToken)}&email={Uri.EscapeDataString(normalizedEmail)}";
            }

            var mail = EduCollabEmailTemplates.WorkspaceInvitation(workspace.Name, acceptUrl, plainToken, hours);
            await _notificationService.SendAsync(
                NotificationMessage.Create(
                    normalizedEmail,
                    NotificationType.WorkspaceInvitation,
                    mail,
                    actions: string.IsNullOrWhiteSpace(acceptUrl)
                        ? null
                        : new[] { new NotificationAction("Accept invitation", acceptUrl) },
                    metadata: new Dictionary<string, string>
                    {
                        ["workspaceId"] = workspaceId.ToString(),
                        ["workspaceName"] = workspace.Name
                    }),
                cancellationToken);

            if (_hostEnvironment.IsDevelopment() && _invitationSettings.Value.LogPlaintextTokenInDevelopment)
            {
                _logger.LogInformation(
                    "Workspace invitation token for {Email} workspace {WorkspaceId}: {Token}",
                    normalizedEmail,
                    workspaceId,
                    plainToken);
            }
        }

        public async Task InviteUserToCurrentWorkspaceAsync(string email, WorkspaceRole role, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanInviteUsers(membership.Role))
            {
                throw new AccessDeniedException("Only workspace owners and managers can send invitations.");
            }

            await InviteUserToWorkspaceAsync(workspaceId, email, role, cancellationToken);
        }

        private static void EnsureInviterCanAssignRole(WorkspaceRole inviterRole, WorkspaceRole assignedRole)
        {
            if (assignedRole == WorkspaceRole.Owner && inviterRole != WorkspaceRole.Owner)
            {
                throw new AccessDeniedException("Only the workspace owner can assign the Owner role.");
            }

            if (inviterRole == WorkspaceRole.Manager && assignedRole is WorkspaceRole.Owner or WorkspaceRole.Manager)
            {
                throw new AccessDeniedException("Managers can only invite users with Creator or Viewer roles.");
            }
        }

        public async Task<bool> CreateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            if (workspace.Id != 0)
                throw new ArgumentException("Workspace Id must not be set when creating.", nameof(workspace));

            var creatorUserId = RequireCurrentUserId();

            var alreadyInWorkspace = await _workspaceRepository.IsUserInAnyWorkspaceAsync(creatorUserId, cancellationToken);
            if (alreadyInWorkspace)
                throw new ArgumentException("You already belong to a workspace.");

            var now = DateTimeOffset.UtcNow;
            workspace.CreatedByUserId = creatorUserId;
            workspace.CreatedAtUtc = now.UtcDateTime;
            workspace.UpdatedAtUtc = now.UtcDateTime;
            workspace.IsArchived = false;

            var id = await _workspaceRepository.CreateWorkspaceWithOwnerAsync(workspace, creatorUserId, now, cancellationToken);
            if (id <= 0)
                return false;

            workspace.Id = id;
            return true;
        }

        public async Task<bool> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            var userId = RequireCurrentUserId();

            var existing = await _workspaceRepository.GetWorkspaceByIdAsync(workspace.Id, cancellationToken);
            if (existing is null)
            {
                return false;
            }

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspace.Id, userId, cancellationToken);
            if (membership is null)
            {
                return false;
            }

            if (!WorkspaceRolePermissions.CanManageWorkspace(membership.Role))
            {
                throw new AccessDeniedException("Only the workspace owner can update the workspace.");
            }

            var isLikelyPartialRequest = workspace.CreatedAtUtc == default;

            if (isLikelyPartialRequest)
            {
                if (!string.IsNullOrWhiteSpace(workspace.Name))
                {
                    existing.Name = workspace.Name.Trim();
                }

                existing.Description = workspace.Description ?? existing.Description;
            }
            else
            {
                existing.Name = string.IsNullOrWhiteSpace(workspace.Name) ? existing.Name : workspace.Name.Trim();
                existing.Description = workspace.Description;
                existing.IsArchived = workspace.IsArchived;
            }

            existing.UpdatedAtUtc = DateTime.UtcNow;

            var updated = await _workspaceRepository.UpdateWorkspaceAsync(existing, userId, cancellationToken);
            if (updated is null)
            {
                return false;
            }

            workspace.Name = updated.Name;
            workspace.Description = updated.Description;
            workspace.UpdatedAtUtc = updated.UpdatedAtUtc;
            workspace.IsArchived = updated.IsArchived;
            workspace.CreatedAtUtc = updated.CreatedAtUtc;
            workspace.CreatedByUserId = updated.CreatedByUserId;
            return true;
        }

        public async Task<bool> UpdateCurrentWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            workspace.Id = workspaceId;
            return await UpdateWorkspaceAsync(workspace, cancellationToken);
        }

        public async Task<bool> DeleteWorkspaceAsync(int workspaceId, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            var userId = RequireCurrentUserId();

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                return false;

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
            if (membership is null)
                return false;

            if (!WorkspaceRolePermissions.CanManageWorkspace(membership.Role))
                throw new AccessDeniedException("Only the workspace owner can delete the workspace.");

            return await _workspaceRepository.SoftDeleteWorkspaceAsync(workspaceId, userId, DateTimeOffset.UtcNow, cancellationToken);
        }

        public async Task<bool> DeleteCurrentWorkspaceAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            return await DeleteWorkspaceAsync(workspaceId, cancellationToken);
        }

        public async Task RemoveWorkspaceMemberAsync(int workspaceId, int targetUserId, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            if (targetUserId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetUserId));

            var actorUserId = RequireCurrentUserId();

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                throw new KeyNotFoundException("Workspace not found.");

            var targetMember = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, targetUserId, cancellationToken);
            if (targetMember is null)
                throw new KeyNotFoundException("That user is not a member of this workspace.");

            var actorMember = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, actorUserId, cancellationToken);
            if (actorMember is null)
                throw new AccessDeniedException("You are not a member of this workspace.");

            var self = actorUserId == targetUserId;
            if (self)
            {
                if (targetMember.Role == WorkspaceRole.Owner)
                    throw new InvalidOperationException("Workspace owners cannot leave via this endpoint; transfer ownership or delete the workspace.");

                var removed = await _workspaceRepository.RemoveWorkspaceMemberAsync(workspaceId, targetUserId, cancellationToken);
                if (!removed)
                    throw new KeyNotFoundException("That user is not a member of this workspace.");

                return;
            }

            if (actorMember.Role == WorkspaceRole.Viewer || actorMember.Role == WorkspaceRole.Creator)
                throw new AccessDeniedException("Only workspace owners and managers can remove other members.");

            if (targetMember.Role == WorkspaceRole.Owner)
                throw new AccessDeniedException("The workspace owner cannot be removed.");

            if (actorMember.Role == WorkspaceRole.Manager && targetMember.Role == WorkspaceRole.Manager)
                throw new AccessDeniedException("Managers can only remove creators and viewers.");

            var ok = await _workspaceRepository.RemoveWorkspaceMemberAsync(workspaceId, targetUserId, cancellationToken);
            if (!ok)
                throw new KeyNotFoundException("That user is not a member of this workspace.");
        }

        public async Task RemoveCurrentWorkspaceMemberAsync(int targetUserId, CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            await RemoveWorkspaceMemberAsync(workspaceId, targetUserId, cancellationToken);
        }

        public async Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            ArgumentNullException.ThrowIfNull(member);

            var actorId = RequireCurrentUserId();

            var existingMember = await _workspaceRepository.GetWorkspaceMemberAsync(id, userId, cancellationToken);
            if (existingMember is null)
            {
                throw new KeyNotFoundException("That user is not a member of this workspace.");
            }

            var actorMember = await _workspaceRepository.GetWorkspaceMemberAsync(id, actorId, cancellationToken);
            if (actorMember is null)
            {
                throw new AccessDeniedException("You are not a member of this workspace.");
            }

            if (actorMember.Role is WorkspaceRole.Viewer or WorkspaceRole.Creator)
            {
                throw new AccessDeniedException("Only workspace owners and managers can change member roles.");
            }

            if (member.Role == WorkspaceRole.Owner && actorMember.Role != WorkspaceRole.Owner)
            {
                throw new AccessDeniedException("Only the workspace owner can assign the Owner role.");
            }

            if (actorMember.Role == WorkspaceRole.Manager)
            {
                if (existingMember.Role is WorkspaceRole.Owner or WorkspaceRole.Manager)
                {
                    throw new AccessDeniedException("Managers can only edit creators and viewers.");
                }

                if (member.Role is WorkspaceRole.Owner or WorkspaceRole.Manager)
                {
                    throw new AccessDeniedException("Managers can only assign Creator or Viewer roles.");
                }
            }

            if (member.Role == WorkspaceRole.Owner)
            {
                await _workspaceRepository.DemoteWorkspaceOwnersExceptAsync(id, userId, cancellationToken);
            }

            return await _workspaceRepository.UpdateWorkspaceMemberAsync(id, userId, member, cancellationToken);
        }

        public async Task<WorkspaceMember?> UpdateCurrentWorkspaceMemberAsync(int userId, WorkspaceMember member, CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            member.WorkspaceId = workspaceId;
            return await UpdateWorkspaceMemberAsync(workspaceId, userId, member, cancellationToken);
        }
    }
}
