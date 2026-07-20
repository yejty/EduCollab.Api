using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Scenes;
using EduCollab.Application.Services.Workspaces;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Sessions
{
    public sealed class SessionService : ISessionService
    {
        private const string EmptySceneJson = "{}";
        private const string AssetsNotIncludedMessage =
            "Asset downloads are not included for this session. Create or update the session with includeAssets enabled.";

        private readonly ISessionRepository _sessionRepository;
        private readonly ISceneRepository _sceneRepository;
        private readonly ISceneContentStore _sceneContentStore;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetContentStore _assetContentStore;
        private readonly IFlowRepository _flowRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupAccessResolver _groupAccessResolver;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ISessionJoinTicketService _joinTicketService;
        private readonly SessionJoinSettings _sessionJoinSettings;

        public SessionService(
            ISessionRepository sessionRepository,
            ISceneRepository sceneRepository,
            ISceneContentStore sceneContentStore,
            IAssetRepository assetRepository,
            IAssetContentStore assetContentStore,
            IFlowRepository flowRepository,
            IGroupRepository groupRepository,
            IGroupAccessResolver groupAccessResolver,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser,
            ISessionJoinTicketService joinTicketService,
            IOptions<SessionJoinSettings> sessionJoinSettings)
        {
            _sessionRepository = sessionRepository;
            _sceneRepository = sceneRepository;
            _sceneContentStore = sceneContentStore;
            _assetRepository = assetRepository;
            _assetContentStore = assetContentStore;
            _flowRepository = flowRepository;
            _groupRepository = groupRepository;
            _groupAccessResolver = groupAccessResolver;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _joinTicketService = joinTicketService;
            _sessionJoinSettings = sessionJoinSettings.Value;
        }

        public async Task<LiveSession> CreateSessionAsync(
            string name,
            string? description,
            int? sceneId,
            int? flowId,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<int> userIds,
            bool includeAssets,
            bool allowGuestLink,
            string? defaultRole,
            CancellationToken cancellationToken)
        {
            var hasScene = sceneId is > 0;
            var hasFlow = flowId is > 0;
            if (hasScene == hasFlow)
                throw new ArgumentException("Exactly one of sceneId or flowId is required.");

            var trimmedName = RequireTrimmed(name, nameof(name));
            var role = NormalizeDefaultRole(defaultRole);

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (hasScene)
            {
                _ = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId!.Value, cancellationToken)
                    ?? throw new KeyNotFoundException("Scene not found.");
            }
            else
            {
                _ = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId!.Value, cancellationToken)
                    ?? throw new KeyNotFoundException("Flow not found.");
            }

            var distinctGroupIds = (groupIds ?? []).Where(id => id > 0).Distinct().ToList();
            var distinctUserIds = (userIds ?? []).Where(id => id > 0 && id != userId).Distinct().ToList();

            foreach (var groupId in distinctGroupIds)
            {
                var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken)
                    ?? throw new ArgumentException($"Group {groupId} was not found.");
                _ = group;
            }

            foreach (var shareUserId in distinctUserIds)
            {
                if (!await _workspaceRepository.IsUserWorkspaceMemberAsync(workspaceId, shareUserId, cancellationToken))
                    throw new ArgumentException($"User {shareUserId} is not a member of this workspace.");
            }

            var now = DateTime.UtcNow;
            var session = new LiveSession
            {
                WorkspaceId = workspaceId,
                HostUserId = userId,
                SceneId = hasScene ? sceneId : null,
                FlowId = hasFlow ? flowId : null,
                Status = LiveSessionStatuses.Pending,
                IncludeAssets = includeAssets,
                AllowGuestLink = allowGuestLink,
                Name = trimmedName,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                DefaultRole = role,
                CreatedAtUtc = now,
            };

            var createdId = await _sessionRepository.CreateSessionAsync(
                session,
                distinctGroupIds,
                distinctUserIds,
                cancellationToken);
            if (createdId <= 0)
                throw new InvalidOperationException("Session could not be created.");

            if (allowGuestLink)
                await MintGuestLinkAsync(createdId, cancellationToken);

            return await GetSessionAsync(createdId, cancellationToken);
        }

        public async Task<List<LiveSession>> ListSessionsAsync(
            string? status,
            string? sharedWith,
            CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var normalizedStatus = NormalizeStatusFilter(status);
            var sharedWithMe = string.IsNullOrWhiteSpace(sharedWith)
                || string.Equals(sharedWith, "me", StringComparison.OrdinalIgnoreCase);

            if (!sharedWithMe)
                throw new ArgumentException("sharedWith must be 'me' when provided.");

            var accessibleGroupIds = await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(
                workspaceId,
                userId,
                cancellationToken);

            var sessions = await _sessionRepository.ListSessionsAsync(
                workspaceId,
                normalizedStatus,
                sharedWithUserId: userId,
                accessibleGroupIds,
                cancellationToken);

            foreach (var session in sessions)
                ApplyAccessMetadata(session, userId, accessibleGroupIds, populateRole: true);

            return sessions;
        }

        public async Task<LiveSession> GetSessionAsync(int sessionId, CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var session = await _sessionRepository.GetSessionByIdAsync(workspaceId, sessionId, cancellationToken)
                ?? throw new KeyNotFoundException("Session not found.");

            var accessibleGroupIds = await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(
                workspaceId,
                userId,
                cancellationToken);
            ApplyAccessMetadata(session, userId, accessibleGroupIds, populateRole: true);

            if (!session.CanJoin && session.HostUserId != userId)
                throw new KeyNotFoundException("Session not found.");

            if (session.HostUserId == userId && session.AllowGuestLink)
                await PopulateGuestLinkUrlAsync(session, cancellationToken);

            return session;
        }

        public async Task<LiveSession> EndSessionAsync(int sessionId, CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            var userId = RequireCurrentUserId();
            if (session.HostUserId != userId)
                throw new AccessDeniedException("Only the session host can end the session.");

            if (session.Status == LiveSessionStatuses.Ended)
                return session;

            await _sessionRepository.EndSessionAsync(session.WorkspaceId, sessionId, cancellationToken);
            return await GetSessionAsync(sessionId, cancellationToken);
        }

        public async Task<SessionJoinTicketResult> CreateJoinTicketAsync(int sessionId, CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            if (session.Status == LiveSessionStatuses.Ended)
                throw new AccessDeniedException("This session has ended.");

            var userId = RequireCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");

            var role = session.HostUserId == userId ? LiveSessionRoles.Host : LiveSessionRoles.Participant;
            var colyseusRole = MapColyseusRole(session, role);

            var ticket = _joinTicketService.CreateTicket(
                session,
                subject: $"user:{userId}",
                displayName: string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
                role: role,
                colyseusRole: colyseusRole);

            await _sessionRepository.AddParticipantAsync(
                session.Id,
                userId,
                guestId: null,
                displayName: string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
                cancellationToken);

            return ticket;
        }

        public async Task<SessionJoinTicketResult> JoinAsGuestAsync(
            string guestToken,
            string displayName,
            CancellationToken cancellationToken)
        {
            var trimmedName = RequireTrimmed(displayName, nameof(displayName));
            if (trimmedName.Length > 200)
                throw new ArgumentException("displayName must be 200 characters or fewer.");

            var (session, link) = await RequireSessionFromGuestTokenAsync(guestToken, cancellationToken);
            if (session.Status == LiveSessionStatuses.Ended)
                throw new AccessDeniedException("This session has ended.");

            if (!await _sessionRepository.TryIncrementGuestLinkUseAsync(link.Id, cancellationToken))
                throw new AccessDeniedException("This guest link is no longer valid.");

            var guestId = Guid.NewGuid().ToString("N");
            var ticket = _joinTicketService.CreateTicket(
                session,
                subject: $"guest:{guestId}",
                displayName: trimmedName,
                role: LiveSessionRoles.Guest,
                colyseusRole: ColyseusRoles.Viewer);

            await _sessionRepository.AddParticipantAsync(
                session.Id,
                userId: null,
                guestId: guestId,
                displayName: trimmedName,
                cancellationToken);

            return ticket;
        }

        public async Task<SessionBootstrap> GetBootstrapAsync(int sessionId, CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            return await BuildBootstrapAsync(session, cancellationToken);
        }

        public async Task<SessionBootstrap> GetBootstrapForGuestAsync(string guestToken, CancellationToken cancellationToken)
        {
            var (session, _) = await RequireSessionFromGuestTokenAsync(guestToken, cancellationToken);
            if (session.Status == LiveSessionStatuses.Ended)
                throw new AccessDeniedException("This session has ended.");

            return await BuildBootstrapAsync(session, cancellationToken, guestToken.Trim());
        }

        public async Task<SessionAssetManifest> GetSessionAssetsAsync(
            int sessionId,
            int sceneId,
            CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            var scene = await RequireSessionSceneAsync(session, sceneId, cancellationToken);
            var assets = await BuildAssetsForSceneAsync(session, scene, cancellationToken);

            return new SessionAssetManifest
            {
                SessionId = session.Id,
                SceneId = sceneId,
                IncludeAssets = session.IncludeAssets,
                Message = session.IncludeAssets ? null : AssetsNotIncludedMessage,
                Assets = assets,
            };
        }

        public async Task<AssetContent> GetSessionAssetContentAsync(
            int sessionId,
            int sceneId,
            int assetId,
            CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            return await GetAssetContentForSessionAsync(session, sceneId, assetId, cancellationToken);
        }

        public async Task<AssetContent> GetSessionAssetContentForGuestAsync(
            string guestToken,
            int sceneId,
            int assetId,
            CancellationToken cancellationToken)
        {
            var (session, _) = await RequireSessionFromGuestTokenAsync(guestToken, cancellationToken);
            return await GetAssetContentForSessionAsync(session, sceneId, assetId, cancellationToken);
        }

        public async Task<string> GetSessionSceneContentAsync(
            int sessionId,
            int sceneId,
            CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);
            var scene = await RequireSessionSceneAsync(session, sceneId, cancellationToken);
            return await LoadSceneContentAsync(session.WorkspaceId, scene.Id, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
        }

        public async Task<string> GetSessionSceneContentForGuestAsync(
            string guestToken,
            int sceneId,
            CancellationToken cancellationToken)
        {
            var (session, _) = await RequireSessionFromGuestTokenAsync(guestToken, cancellationToken);
            var scene = await RequireSessionSceneAsync(session, sceneId, cancellationToken);
            return await LoadSceneContentAsync(session.WorkspaceId, scene.Id, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
        }

        public async Task MarkRoomStartedAsync(int sessionId, CancellationToken cancellationToken)
        {
            if (sessionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId));

            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status == LiveSessionStatuses.Ended)
                throw new AccessDeniedException("This session has ended.");

            await _sessionRepository.MarkRoomStartedAsync(sessionId, cancellationToken);
        }

        public async Task MarkRoomEndedAsync(int sessionId, CancellationToken cancellationToken)
        {
            if (sessionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId));

            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status == LiveSessionStatuses.Ended)
                return;

            await _sessionRepository.MarkRoomEndedAsync(sessionId, cancellationToken);
        }

        public Task<int> EndAbandonedSessionsAsync(CancellationToken cancellationToken)
        {
            var idleHours = Math.Max(1, _sessionJoinSettings.AbandonedSessionIdleHours);
            var olderThanUtc = DateTime.UtcNow.AddHours(-idleHours);
            return _sessionRepository.EndAbandonedSessionsAsync(olderThanUtc, cancellationToken);
        }

        private async Task<SessionBootstrap> BuildBootstrapAsync(
            LiveSession session,
            CancellationToken cancellationToken,
            string? guestToken = null)
        {
            if (session.Status == LiveSessionStatuses.Ended)
                throw new AccessDeniedException("This session has ended.");

            var bootstrap = new SessionBootstrap
            {
                SessionId = session.Id,
                Name = session.Name,
                Status = session.Status,
                IncludeAssets = session.IncludeAssets,
                ColyseusEndpoint = string.IsNullOrWhiteSpace(_sessionJoinSettings.ColyseusEndpoint)
                    ? null
                    : _sessionJoinSettings.ColyseusEndpoint.Trim(),
                Message = session.IncludeAssets ? null : AssetsNotIncludedMessage,
            };

            if (session.SceneId is int sceneId)
            {
                bootstrap.AssetKind = "scene";
                bootstrap.AssetId = sceneId.ToString();
                bootstrap.AssetName = session.SceneName;

                var scene = await RequireSessionSceneAsync(session, sceneId, cancellationToken);
                bootstrap.Scenes.Add(ToBootstrapScene(session.Id, scene, guestToken));
                bootstrap.Assets.AddRange(await BuildAssetsForSceneAsync(session, scene, cancellationToken, guestToken));
            }
            else if (session.FlowId is int flowId)
            {
                bootstrap.AssetKind = "flow";
                bootstrap.AssetId = flowId.ToString();
                bootstrap.AssetName = session.FlowName;

                var links = await _flowRepository.GetFlowSceneLinksAsync(session.WorkspaceId, flowId, cancellationToken);
                foreach (var link in links)
                {
                    var scene = await RequireSessionSceneAsync(session, link.SceneId, cancellationToken);
                    bootstrap.Scenes.Add(ToBootstrapScene(session.Id, scene, guestToken));
                    bootstrap.Assets.AddRange(await BuildAssetsForSceneAsync(session, scene, cancellationToken, guestToken));
                }
            }

            return bootstrap;
        }

        private async Task<AssetContent> GetAssetContentForSessionAsync(
            LiveSession session,
            int sceneId,
            int assetId,
            CancellationToken cancellationToken)
        {
            if (!session.IncludeAssets)
                throw new AccessDeniedException(AssetsNotIncludedMessage, "assets_not_included");

            var scene = await RequireSessionSceneAsync(session, sceneId, cancellationToken);
            var json = await LoadSceneContentAsync(session.WorkspaceId, scene.Id, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
            var referenced = SceneJsonAssetReferenceParser.ExtractAssetIds(json);
            if (!referenced.Contains(assetId))
                throw new KeyNotFoundException("Asset not found in this session scene.");

            return await _assetContentStore.GetAsync(session.WorkspaceId, assetId, cancellationToken)
                ?? throw new KeyNotFoundException("Asset content not found.");
        }

        private async Task<(LiveSession Session, SessionGuestLink Link)> RequireSessionFromGuestTokenAsync(
            string guestToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(guestToken))
                throw new ArgumentException("guestToken is required.", nameof(guestToken));

            var tokenHash = RefreshTokenGenerator.HashPlaintext(guestToken.Trim());
            var link = await _sessionRepository.GetActiveGuestLinkByTokenHashAsync(tokenHash, cancellationToken)
                ?? throw new KeyNotFoundException("Guest link not found.");

            if (link.MaxUses is int maxUses && link.UseCount >= maxUses)
                throw new AccessDeniedException("This guest link has reached its maximum number of uses.");

            var session = await _sessionRepository.GetSessionByIdAsync(link.SessionId, cancellationToken)
                ?? throw new KeyNotFoundException("Session not found.");

            if (!session.AllowGuestLink)
                throw new AccessDeniedException("Guest links are not enabled for this session.");

            return (session, link);
        }

        private async Task MintGuestLinkAsync(int sessionId, CancellationToken cancellationToken)
        {
            var plaintext = RefreshTokenGenerator.Create();
            var hours = _sessionJoinSettings.GuestLinkExpirationHours;
            var link = new SessionGuestLink
            {
                SessionId = sessionId,
                TokenHash = RefreshTokenGenerator.HashPlaintext(plaintext),
                TokenPlaintext = plaintext,
                ExpiresAtUtc = hours > 0 ? DateTime.UtcNow.AddHours(hours) : null,
                MaxUses = null,
                UseCount = 0,
                CreatedAtUtc = DateTime.UtcNow,
            };
            await _sessionRepository.UpsertGuestLinkAsync(link, cancellationToken);
        }

        private async Task PopulateGuestLinkUrlAsync(LiveSession session, CancellationToken cancellationToken)
        {
            var link = await _sessionRepository.GetActiveGuestLinkBySessionIdAsync(session.Id, cancellationToken);
            if (link is null || string.IsNullOrWhiteSpace(link.TokenPlaintext))
            {
                await MintGuestLinkAsync(session.Id, cancellationToken);
                link = await _sessionRepository.GetActiveGuestLinkBySessionIdAsync(session.Id, cancellationToken);
            }

            if (link is null || string.IsNullOrWhiteSpace(link.TokenPlaintext))
                return;

            session.GuestLinkUrl = BuildGuestLinkUrl(link.TokenPlaintext);
        }

        private string? BuildGuestLinkUrl(string guestToken)
        {
            var baseUrl = _sessionJoinSettings.FrontendGuestJoinUrl?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            return $"{baseUrl}/{Uri.EscapeDataString(guestToken)}";
        }

        private async Task<Scene> RequireSessionSceneAsync(
            LiveSession session,
            int sceneId,
            CancellationToken cancellationToken)
        {
            if (session.SceneId is int boundSceneId)
            {
                if (boundSceneId != sceneId)
                    throw new KeyNotFoundException("Scene not found.");
            }
            else if (session.FlowId is int flowId)
            {
                var links = await _flowRepository.GetFlowSceneLinksAsync(session.WorkspaceId, flowId, cancellationToken);
                if (!links.Any(link => link.SceneId == sceneId))
                    throw new KeyNotFoundException("Scene not found.");
            }
            else
            {
                throw new KeyNotFoundException("Scene not found.");
            }

            return await _sceneRepository.GetSceneByIdAsync(session.WorkspaceId, sceneId, cancellationToken)
                ?? throw new KeyNotFoundException("Scene not found.");
        }

        private async Task<List<SessionBootstrapAsset>> BuildAssetsForSceneAsync(
            LiveSession session,
            Scene scene,
            CancellationToken cancellationToken,
            string? guestToken = null)
        {
            var json = await LoadSceneContentAsync(session.WorkspaceId, scene.Id, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
            var referencedAssetIds = SceneJsonAssetReferenceParser.ExtractAssetIds(json);
            var assets = new List<SessionBootstrapAsset>();

            foreach (var assetId in referencedAssetIds.OrderBy(id => id))
            {
                var asset = await _assetRepository.GetAssetByIdAsync(session.WorkspaceId, assetId, cancellationToken);
                if (asset is null)
                    continue;

                string? contentUrl = null;
                if (session.IncludeAssets)
                {
                    contentUrl = string.IsNullOrWhiteSpace(guestToken)
                        ? $"/api/workspace/session-assets/content?sessionId={session.Id}&sceneId={scene.Id}&assetId={asset.Id}"
                        : $"/api/public/session-assets/content?guestToken={Uri.EscapeDataString(guestToken)}&sceneId={scene.Id}&assetId={asset.Id}";
                }

                assets.Add(new SessionBootstrapAsset
                {
                    AssetId = asset.Id,
                    SceneId = scene.Id,
                    Name = asset.Name,
                    AssetType = asset.AssetType,
                    DownloadAvailable = session.IncludeAssets,
                    ContentUrl = contentUrl,
                    CacheKey = session.IncludeAssets
                        ? $"{session.WorkspaceId}:{asset.Id}:{asset.UpdatedAtUtc.Ticks}"
                        : null,
                    Message = session.IncludeAssets ? null : AssetsNotIncludedMessage,
                });
            }

            return assets;
        }

        private static SessionBootstrapScene ToBootstrapScene(int sessionId, Scene scene, string? guestToken = null) =>
            new()
            {
                SceneId = scene.Id,
                Name = scene.Name,
                ContentUrl = string.IsNullOrWhiteSpace(guestToken)
                    ? $"/api/workspace/session-scenes/content?sessionId={sessionId}&sceneId={scene.Id}"
                    : $"/api/public/session-scenes/content?guestToken={Uri.EscapeDataString(guestToken)}&sceneId={scene.Id}",
            };

        private async Task<string?> LoadSceneContentAsync(
            int workspaceId,
            int sceneId,
            string? legacyJsonContent,
            CancellationToken cancellationToken)
        {
            var storedContent = await _sceneContentStore.GetAsync(workspaceId, sceneId, cancellationToken);
            if (storedContent is not null)
                return storedContent;

            if (string.IsNullOrWhiteSpace(legacyJsonContent) || legacyJsonContent == EmptySceneJson)
                return null;

            await _sceneContentStore.SaveAsync(workspaceId, sceneId, legacyJsonContent, cancellationToken);
            return legacyJsonContent;
        }

        private static void ApplyAccessMetadata(
            LiveSession session,
            int userId,
            HashSet<int> accessibleGroupIds,
            bool populateRole)
        {
            var isHost = session.HostUserId == userId;
            var hasUserShare = session.UserIds.Contains(userId);
            var hasGroupShare = session.GroupIds.Any(accessibleGroupIds.Contains);
            session.CanJoin = isHost || hasUserShare || hasGroupShare;

            if (!populateRole)
                return;

            if (isHost)
            {
                session.EffectiveRole = LiveSessionRoles.Host;
            }
            else if (session.CanJoin)
            {
                session.EffectiveRole = LiveSessionRoles.Participant;
            }
        }

        private static string MapColyseusRole(LiveSession session, string role)
        {
            if (role == LiveSessionRoles.Host)
                return ColyseusRoles.Host;

            return string.IsNullOrWhiteSpace(session.DefaultRole)
                ? ColyseusRoles.Viewer
                : session.DefaultRole;
        }

        private static string NormalizeDefaultRole(string? defaultRole)
        {
            if (string.IsNullOrWhiteSpace(defaultRole))
                return ColyseusRoles.Viewer;

            var normalized = defaultRole.Trim().ToLowerInvariant();
            return normalized switch
            {
                ColyseusRoles.Viewer or ColyseusRoles.Editor or ColyseusRoles.Presenter => normalized,
                _ => throw new ArgumentException("defaultRole must be viewer, editor, or presenter."),
            };
        }

        private static string? NormalizeStatusFilter(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var normalized = status.Trim();
            if (normalized.Equals(LiveSessionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                return LiveSessionStatuses.Pending;
            if (normalized.Equals(LiveSessionStatuses.Active, StringComparison.OrdinalIgnoreCase))
                return LiveSessionStatuses.Active;
            if (normalized.Equals(LiveSessionStatuses.Ended, StringComparison.OrdinalIgnoreCase))
                return LiveSessionStatuses.Ended;

            throw new ArgumentException("status must be Pending, Active, or Ended.");
        }

        private static string RequireTrimmed(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} is required.", paramName);

            return value.Trim();
        }

        private int RequireCurrentUserId() =>
            _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");

        private Task<(int WorkspaceId, WorkspaceMember Membership)> RequireWorkspaceMembershipAsync(
            CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            return CurrentWorkspaceAccess.RequireMembershipAsync(
                _userRepository,
                _workspaceRepository,
                userId,
                cancellationToken);
        }
    }
}
