using System.Text;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Security;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Application.Services.Flows
{
    public interface IFlowService
    {
        Task<bool> CreateFlowAsync(Flow flow, IReadOnlyList<int> groupIds, IReadOnlyList<int>? sceneIds, CancellationToken cancellationToken);
        Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken);
        Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsInGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken);
        Task<Flow?> UpdateFlowAsync(Flow flow, IReadOnlyList<int>? sceneIds, CancellationToken cancellationToken);
        Task<bool> DeleteFlowAsync(int flowId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageFlowAsync(int ownerUserId, CancellationToken cancellationToken);
        Task<List<FlowGroupShare>> GetFlowGroupSharesAsync(int flowId, CancellationToken cancellationToken);
        Task<List<FlowGroupShare>?> SetFlowGroupSharesAsync(int flowId, IReadOnlyList<FlowGroupShare> shares, CancellationToken cancellationToken);
        Task<bool> AddFlowGroupAsync(int flowId, int groupId, bool includeAssets, CancellationToken cancellationToken);
        Task<bool> RemoveFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken);
        Task<FlowScenesManifest> GetFlowScenesAsync(int flowId, CancellationToken cancellationToken);
        Task<FlowSceneContent?> GetFlowSceneContentAsync(string downloadToken, CancellationToken cancellationToken);
    }

    public class FlowService : IFlowService
    {
        private const string EmptySceneJson = "{}";
        private const string ScenesNotIncludedMessage =
            "Included content links are missing. Share this flow with includeAssets enabled to allow downloads.";

        private readonly IFlowRepository _flowRepository;
        private readonly ISceneRepository _sceneRepository;
        private readonly ISceneContentStore _sceneContentStore;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupAccessResolver _groupAccessResolver;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IContentDownloadTokenService _contentDownloadTokenService;

        public FlowService(
            IFlowRepository flowRepository,
            ISceneRepository sceneRepository,
            ISceneContentStore sceneContentStore,
            IGroupRepository groupRepository,
            IGroupAccessResolver groupAccessResolver,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser,
            IContentDownloadTokenService contentDownloadTokenService)
        {
            _flowRepository = flowRepository;
            _sceneRepository = sceneRepository;
            _sceneContentStore = sceneContentStore;
            _groupRepository = groupRepository;
            _groupAccessResolver = groupAccessResolver;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _contentDownloadTokenService = contentDownloadTokenService;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private static string RequireTrimmed(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} is required.", paramName);

            return value.Trim();
        }

        private Task<(int WorkspaceId, WorkspaceMember Membership)> RequireWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            return CurrentWorkspaceAccess.RequireMembershipAsync(
                _userRepository,
                _workspaceRepository,
                userId,
                cancellationToken);
        }

        private static void EnsureCanLoadFlows(WorkspaceMember membership)
        {
            if (!WorkspaceParameterPermissions.CanLoadFlows(membership))
                throw new AccessDeniedException("You do not have permission to load flows.");
        }

        private async Task<HashSet<int>> GetAccessibleGroupIdsAsync(int workspaceId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceParameterPermissions.CanSeeAllContent(membership))
            {
                var allGroups = await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);
                return allGroups.Select(g => g.Id).ToHashSet();
            }

            return await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(workspaceId, userId, cancellationToken);
        }

        private async Task EnsureGroupBelongsToWorkspaceAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");
        }

        private async Task EnsureCanPlaceInGroupAsync(int workspaceId, int groupId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceParameterPermissions.CanSeeAllContent(membership))
                return;

            if (await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))
                return;

            throw new AccessDeniedException("You do not have access to place resources in this group.");
        }

        private static bool CanManageFlow(WorkspaceMember membership, int ownerUserId, int userId)
        {
            if (WorkspaceParameterPermissions.CanSeeAllContent(membership))
                return true;

            if (!WorkspaceParameterPermissions.CanCreateFlows(membership))
                return false;

            return ownerUserId == userId;
        }

        private async Task EnsureCanManageFlowAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (CanManageFlow(membership, ownerUserId, RequireCurrentUserId()))
                return;

            throw new AccessDeniedException("You do not have permission to manage this flow.");
        }

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

        private async Task EnsureValidFlowSceneReferencesAsync(
            int workspaceId,
            WorkspaceMember membership,
            int userId,
            IReadOnlyList<int> sceneIds,
            CancellationToken cancellationToken)
        {
            if (sceneIds.Count == 0)
                throw new ArgumentException("At least one sceneId is required.", nameof(sceneIds));

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            var canSeeAllContent = WorkspaceParameterPermissions.CanSeeAllContent(membership);
            var invalidReferences = new List<InvalidSceneReference>();

            foreach (var sceneId in sceneIds.Distinct())
            {
                var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
                if (scene is null)
                {
                    invalidReferences.Add(new InvalidSceneReference(sceneId, "Scene was not found in this workspace."));
                    continue;
                }

                await ContentGroupShareOperations.PopulateSceneGroupIdsAsync(
                    _sceneRepository,
                    workspaceId,
                    scene,
                    cancellationToken);

                if (!WorkspaceContentVisibility.IsSceneVisibleToUser(scene, userId, canSeeAllContent, accessibleGroupIds))
                    invalidReferences.Add(new InvalidSceneReference(sceneId, "Scene is not accessible to the current user."));
            }

            if (invalidReferences.Count > 0)
                throw new InvalidSceneReferenceException(invalidReferences);
        }

        private async Task<bool> HasEffectiveFlowIncludeAssetsAsync(
            int workspaceId,
            int flowId,
            IReadOnlySet<int> accessibleGroupIds,
            CancellationToken cancellationToken)
        {
            var shares = await _flowRepository.GetFlowGroupSharesAsync(workspaceId, flowId, cancellationToken);
            foreach (var share in shares)
            {
                if (share.IncludeAssets && accessibleGroupIds.Contains(share.GroupId))
                    return true;
            }

            return false;
        }

        public async Task<bool> CreateFlowAsync(Flow flow, IReadOnlyList<int> groupIds, IReadOnlyList<int>? sceneIds, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(flow);

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceParameterPermissions.CanCreateFlows(membership))
                throw new AccessDeniedException("Viewers have read-only access to flows.");

            var userId = RequireCurrentUserId();
            var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(groupIds);
            await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                _groupRepository,
                _groupAccessResolver,
                workspaceId,
                resolvedGroupIds,
                membership,
                userId,
                cancellationToken);

            var resolvedSceneIds = sceneIds?.ToList() ?? [];
            await EnsureValidFlowSceneReferencesAsync(workspaceId, membership, userId, resolvedSceneIds, cancellationToken);

            flow.WorkspaceId = workspaceId;
            flow.GroupId = ResourceGroupPlacement.PrimaryGroupId(resolvedGroupIds);
            flow.GroupIds = resolvedGroupIds.ToList();
            flow.OwnerUserId = userId;
            flow.Name = RequireTrimmed(flow.Name, nameof(flow.Name));
            flow.Description = string.IsNullOrWhiteSpace(flow.Description) ? null : flow.Description.Trim();
            flow.CreatedAtUtc = DateTime.UtcNow;
            flow.UpdatedAtUtc = flow.CreatedAtUtc;

            var id = await _flowRepository.CreateFlowAsync(workspaceId, flow, cancellationToken);
            if (id <= 0)
                return false;

            flow.Id = id;
            if (resolvedGroupIds.Count > 0)
            {
                await _flowRepository.ReplaceFlowGroupSharesAsync(workspaceId, id, resolvedGroupIds, cancellationToken);
                await _flowRepository.SyncFlowPrimaryGroupIdAsync(workspaceId, id, cancellationToken);
            }

            await _flowRepository.ReplaceFlowSceneLinksAsync(workspaceId, id, resolvedSceneIds, userId, cancellationToken);

            flow.SceneIds = resolvedSceneIds;
            return true;
        }

        public async Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanLoadFlows(membership);
            var userId = RequireCurrentUserId();
            var flows = await _flowRepository.GetAllFlowsAsync(workspaceId, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            var visibleFlows = flows
                .Where(flow => WorkspaceContentVisibility.IsFlowVisibleToUser(flow, userId, WorkspaceParameterPermissions.CanSeeAllContent(membership), accessibleGroupIds))
                .ToList();
            await ContentGroupShareOperations.PopulateFlowSceneIdsAsync(_flowRepository, workspaceId, visibleFlows, cancellationToken);
            ContentGroupShareOperations.RedactResourceGroupSharesIfLoadScenesAndFlowsOnly(visibleFlows, membership);
            return visibleFlows;
        }

        public async Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanLoadFlows(membership);
            var userId = RequireCurrentUserId();
            var flows = await _flowRepository.GetFlowsByOwnerAsync(workspaceId, userId, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowSceneIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            ContentGroupShareOperations.RedactResourceGroupSharesIfLoadScenesAndFlowsOnly(flows, membership);
            return flows;
        }

        public async Task<List<Flow>> GetFlowsInGroupAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanLoadFlows(membership);
            var userId = RequireCurrentUserId();

            if (!WorkspaceParameterPermissions.CanSeeAllContent(membership)
                && !await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))
            {
                throw new AccessDeniedException("You do not have access to this group.");
            }

            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            var flows = await _flowRepository.GetFlowsByGroupAsync(workspaceId, groupId, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowSceneIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            ContentGroupShareOperations.RedactResourceGroupSharesIfLoadScenesAndFlowsOnly(flows, membership);
            return flows;
        }

        public async Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanLoadFlows(membership);
            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return null;

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flow, cancellationToken);

            var userId = RequireCurrentUserId();
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            if (!WorkspaceContentVisibility.IsFlowVisibleToUser(flow, userId, WorkspaceParameterPermissions.CanSeeAllContent(membership), accessibleGroupIds))
                return null;

            await ContentGroupShareOperations.PopulateFlowSceneIdsAsync(_flowRepository, workspaceId, flow, cancellationToken);
            ContentGroupShareOperations.RedactResourceGroupSharesIfLoadScenesAndFlowsOnly(flow, membership);
            return flow;
        }

        public async Task<Flow?> UpdateFlowAsync(Flow flow, IReadOnlyList<int>? sceneIds, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(flow);
            if (flow.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(flow.Id));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flow.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);

            var userId = RequireCurrentUserId();

            if (sceneIds is not null)
            {
                var resolvedSceneIds = sceneIds.ToList();
                await EnsureValidFlowSceneReferencesAsync(workspaceId, membership, userId, resolvedSceneIds, cancellationToken);
                await _flowRepository.ReplaceFlowSceneLinksAsync(workspaceId, flow.Id, resolvedSceneIds, userId, cancellationToken);
            }

            existing.Name = RequireTrimmed(flow.Name, nameof(flow.Name));
            existing.Description = string.IsNullOrWhiteSpace(flow.Description) ? null : flow.Description.Trim();

            var updated = await _flowRepository.UpdateFlowAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, updated, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowSceneIdsAsync(_flowRepository, workspaceId, updated, cancellationToken);
            return updated;
        }

        public async Task<bool> DeleteFlowAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);
            return await _flowRepository.DeleteFlowAsync(workspaceId, flowId, cancellationToken);
        }

        public async Task<bool> CanCurrentUserManageFlowAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            try
            {
                var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
                return CanManageFlow(membership, ownerUserId, RequireCurrentUserId());
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (AccessDeniedException)
            {
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public async Task<List<FlowGroupShare>> GetFlowGroupSharesAsync(int flowId, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceParameterPermissions.CanViewFlowGroupShares(membership))
                throw new AccessDeniedException("You do not have permission to view flow group shares.");

            var flow = await GetFlowByIdAsync(flowId, cancellationToken);
            if (flow is null)
                throw new KeyNotFoundException("Flow not found.");

            return await _flowRepository.GetFlowGroupSharesAsync(workspaceId, flowId, cancellationToken);
        }

        public async Task<List<FlowGroupShare>?> SetFlowGroupSharesAsync(
            int flowId,
            IReadOnlyList<FlowGroupShare> shares,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(shares);

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);

            var resolvedShares = shares
                .Where(share => share.GroupId > 0)
                .GroupBy(share => share.GroupId)
                .Select(group => group.Last())
                .ToList();

            var resolvedGroupIds = resolvedShares.Select(share => share.GroupId).ToList();
            await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                _groupRepository,
                _groupAccessResolver,
                workspaceId,
                resolvedGroupIds,
                membership,
                RequireCurrentUserId(),
                cancellationToken);

            await _flowRepository.ReplaceFlowGroupSharesAsync(workspaceId, flowId, resolvedShares, cancellationToken);
            await _flowRepository.SyncFlowPrimaryGroupIdAsync(workspaceId, flowId, cancellationToken);
            return await _flowRepository.GetFlowGroupSharesAsync(workspaceId, flowId, cancellationToken);
        }

        public async Task<bool> AddFlowGroupAsync(int flowId, int groupId, bool includeAssets, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, groupId, membership, RequireCurrentUserId(), cancellationToken);

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, existing, cancellationToken);
            var added = await _flowRepository.AddFlowGroupShareAsync(workspaceId, flowId, groupId, includeAssets, cancellationToken);
            if (!added && !existing.GroupIds.Contains(groupId))
                return false;

            await _flowRepository.SyncFlowPrimaryGroupIdAsync(workspaceId, flowId, cancellationToken);
            return true;
        }

        public async Task<bool> RemoveFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);

            var removed = await _flowRepository.RemoveFlowGroupShareAsync(workspaceId, flowId, groupId, cancellationToken);
            if (!removed)
                return false;

            await _flowRepository.SyncFlowPrimaryGroupIdAsync(workspaceId, flowId, cancellationToken);
            return true;
        }

        public async Task<FlowScenesManifest> GetFlowScenesAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanLoadFlows(membership);
            var userId = RequireCurrentUserId();

            var flow = await GetFlowByIdAsync(flowId, cancellationToken);
            if (flow is null)
                throw new KeyNotFoundException("Flow not found.");

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            var includeAssets = await HasEffectiveFlowIncludeAssetsAsync(
                workspaceId,
                flowId,
                accessibleGroupIds,
                cancellationToken);

            var canSeeAllContent = WorkspaceParameterPermissions.CanSeeAllContent(membership);
            var canLoadScenes = WorkspaceParameterPermissions.CanLoadScenes(membership);
            var items = new List<FlowSceneContextItem>();

            foreach (var sceneId in flow.SceneIds.OrderBy(id => id))
            {
                var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
                if (scene is null)
                    continue;

                await ContentGroupShareOperations.PopulateSceneGroupIdsAsync(
                    _sceneRepository,
                    workspaceId,
                    scene,
                    cancellationToken);

                var canViewDirectly = canLoadScenes
                    && WorkspaceContentVisibility.IsSceneVisibleToUser(scene, userId, canSeeAllContent, accessibleGroupIds);

                var usableInFlow = canViewDirectly || includeAssets;
                string? downloadToken = null;
                string? cacheKey = null;
                string? message = null;

                if (!canViewDirectly && includeAssets)
                {
                    downloadToken = _contentDownloadTokenService.CreateSceneContentToken(
                        userId,
                        workspaceId,
                        flowId,
                        sceneId);
                    cacheKey = Guid.NewGuid().ToString("N");
                }
                else if (!canViewDirectly && !includeAssets)
                {
                    message = ScenesNotIncludedMessage;
                }

                items.Add(new FlowSceneContextItem
                {
                    SceneId = scene.Id,
                    FlowId = flowId,
                    WorkspaceId = workspaceId,
                    Name = scene.Name,
                    UsableInFlow = usableInFlow,
                    CanViewDirectly = canViewDirectly,
                    IncludeAssets = includeAssets,
                    DownloadToken = downloadToken,
                    CacheKey = cacheKey,
                    Message = message,
                });
            }

            return new FlowScenesManifest
            {
                FlowId = flowId,
                IncludeAssets = includeAssets,
                Message = includeAssets ? null : ScenesNotIncludedMessage,
                Scenes = items
                    .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.SceneId)
                    .ToList(),
            };
        }

        public async Task<FlowSceneContent?> GetFlowSceneContentAsync(string downloadToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(downloadToken))
                throw new AccessDeniedException(ScenesNotIncludedMessage, "scenes_not_included");

            var userId = RequireCurrentUserId();
            if (!_contentDownloadTokenService.TryValidateSceneContentToken(
                    downloadToken,
                    userId,
                    out var workspaceId,
                    out var flowId,
                    out var sceneId))
            {
                throw new AccessDeniedException(
                    "The download token is invalid or expired. Refresh the flow-scenes manifest and try again.",
                    "scenes_not_included");
            }

            var (activeWorkspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (activeWorkspaceId != workspaceId)
                throw new AccessDeniedException("The download token does not match the active workspace.", "scenes_not_included");

            EnsureCanLoadFlows(membership);

            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return null;

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flow, cancellationToken);
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            if (!WorkspaceContentVisibility.IsFlowVisibleToUser(
                    flow,
                    userId,
                    WorkspaceParameterPermissions.CanSeeAllContent(membership),
                    accessibleGroupIds))
            {
                return null;
            }

            await ContentGroupShareOperations.PopulateFlowSceneIdsAsync(_flowRepository, workspaceId, flow, cancellationToken);
            if (!flow.SceneIds.Contains(sceneId))
                return null;

            var includeAssets = await HasEffectiveFlowIncludeAssetsAsync(
                workspaceId,
                flowId,
                accessibleGroupIds,
                cancellationToken);
            if (!includeAssets)
                throw new AccessDeniedException(ScenesNotIncludedMessage, "scenes_not_included");

            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            var jsonContent = await LoadSceneContentAsync(workspaceId, sceneId, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;

            return new FlowSceneContent
            {
                SceneId = sceneId,
                ContentType = "application/json",
                FileName = "scene.json",
                Data = Encoding.UTF8.GetBytes(jsonContent),
            };
        }
    }
}
