using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Application.Services.Flows
{
    public interface IFlowService
    {
        Task<bool> CreateFlowAsync(Flow flow, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken);
        Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsInGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken);
        Task<Flow?> UpdateFlowAsync(Flow flow, IReadOnlyList<int>? groupIds, CancellationToken cancellationToken);
        Task<bool> DeleteFlowAsync(int flowId, CancellationToken cancellationToken);
        Task<List<FlowSceneContextItem>> GetFlowScenesAsync(int flowId, CancellationToken cancellationToken);
        Task<FlowSceneContextItem?> AttachFlowSceneAsync(int flowId, int sceneId, CancellationToken cancellationToken);
        Task<bool> DetachFlowSceneAsync(int flowId, int sceneId, CancellationToken cancellationToken);
        Task<string?> GetFlowSceneContentAsync(int flowId, int sceneId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageFlowAsync(int ownerUserId, CancellationToken cancellationToken);
        Task<List<int>> GetFlowGroupIdsAsync(int flowId, CancellationToken cancellationToken);
        Task<List<int>?> SetFlowGroupIdsAsync(int flowId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task<bool> AddFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken);
        Task<bool> RemoveFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken);
    }

    public class FlowService : IFlowService
    {
        private const string EmptySceneJson = "{}";

        private readonly IFlowRepository _flowRepository;
        private readonly ISceneRepository _sceneRepository;
        private readonly ISceneContentStore _sceneContentStore;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupAccessResolver _groupAccessResolver;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public FlowService(
            IFlowRepository flowRepository,
            ISceneRepository sceneRepository,
            ISceneContentStore sceneContentStore,
            IGroupRepository groupRepository,
            IGroupAccessResolver groupAccessResolver,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _flowRepository = flowRepository;
            _sceneRepository = sceneRepository;
            _sceneContentStore = sceneContentStore;
            _groupRepository = groupRepository;
            _groupAccessResolver = groupAccessResolver;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
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

        private async Task<bool> IsSceneAttachedToFlowAsync(
            int workspaceId,
            int flowId,
            int sceneId,
            CancellationToken cancellationToken)
        {
            var links = await _flowRepository.GetFlowSceneLinksAsync(workspaceId, flowId, cancellationToken);
            return links.Any(link => link.SceneId == sceneId);
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

        private async Task<HashSet<int>> GetAccessibleGroupIdsAsync(int workspaceId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
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
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return;

            if (await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))
                return;

            throw new AccessDeniedException("You do not have access to place resources in this group.");
        }

        private static bool CanManageFlow(WorkspaceMember membership, int ownerUserId, int userId)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return true;

            if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
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

        public async Task<bool> CreateFlowAsync(Flow flow, IReadOnlyList<int> groupIds, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(flow);

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
                throw new AccessDeniedException("Viewers have read-only access to flows.");

            var userId = RequireCurrentUserId();
            var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(flow.GroupId, groupIds.ToList());
            await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                _groupRepository,
                _groupAccessResolver,
                workspaceId,
                resolvedGroupIds,
                membership,
                userId,
                cancellationToken);

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

            return true;
        }

        public async Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var flows = await _flowRepository.GetAllFlowsAsync(workspaceId, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return flows
                .Where(flow => WorkspaceContentVisibility.IsFlowVisibleToUser(flow, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds))
                .ToList();
        }

        public async Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var flows = await _flowRepository.GetFlowsByOwnerAsync(workspaceId, userId, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            return flows;
        }

        public async Task<List<Flow>> GetFlowsInGroupAsync(int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (!WorkspaceRolePermissions.CanSeeAllContent(membership.Role)
                && !await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))
            {
                throw new AccessDeniedException("You do not have access to this group.");
            }

            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            var flows = await _flowRepository.GetFlowsByGroupAsync(workspaceId, groupId, cancellationToken);
            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flows, cancellationToken);
            return flows;
        }

        public async Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return null;

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, flow, cancellationToken);

            var userId = RequireCurrentUserId();
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return WorkspaceContentVisibility.IsFlowVisibleToUser(flow, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds)
                ? flow
                : null;
        }

        public async Task<Flow?> UpdateFlowAsync(Flow flow, IReadOnlyList<int>? groupIds, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(flow);
            if (flow.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(flow.Id));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flow.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);

            if (groupIds is not null)
            {
                var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(flow.GroupId, groupIds.ToList());
                await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                    _groupRepository,
                    _groupAccessResolver,
                    workspaceId,
                    resolvedGroupIds,
                    membership,
                    RequireCurrentUserId(),
                    cancellationToken);
                await _flowRepository.ReplaceFlowGroupSharesAsync(workspaceId, flow.Id, resolvedGroupIds, cancellationToken);
                await _flowRepository.SyncFlowPrimaryGroupIdAsync(workspaceId, flow.Id, cancellationToken);
            }

            existing.Name = RequireTrimmed(flow.Name, nameof(flow.Name));
            existing.Description = string.IsNullOrWhiteSpace(flow.Description) ? null : flow.Description.Trim();

            var updated = await _flowRepository.UpdateFlowAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, updated, cancellationToken);
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

        public async Task<List<FlowSceneContextItem>> GetFlowScenesAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var flow = await GetFlowByIdAsync(flowId, cancellationToken);
            if (flow is null)
                throw new KeyNotFoundException("Flow not found.");

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            var canSeeAll = WorkspaceRolePermissions.CanSeeAllContent(membership.Role);
            var links = await _flowRepository.GetFlowSceneLinksAsync(workspaceId, flowId, cancellationToken);

            var items = new List<FlowSceneContextItem>();
            foreach (var link in links)
            {
                var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, link.SceneId, cancellationToken);
                if (scene is null)
                    continue;

                await ContentGroupShareOperations.PopulateSceneGroupIdsAsync(_sceneRepository, workspaceId, scene, cancellationToken);
                items.Add(BuildFlowSceneContextItem(flowId, workspaceId, scene, userId, canSeeAll, accessibleGroupIds));
            }

            return items
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.SceneId)
                .ToList();
        }

        public async Task<FlowSceneContextItem?> AttachFlowSceneAsync(int flowId, int sceneId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return null;

            await EnsureCanManageFlowAsync(flow.OwnerUserId, cancellationToken);

            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            await ContentGroupShareOperations.PopulateSceneGroupIdsAsync(_sceneRepository, workspaceId, scene, cancellationToken);

            var link = new FlowSceneLink
            {
                FlowId = flowId,
                SceneId = sceneId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _flowRepository.CreateFlowSceneLinkAsync(workspaceId, link, cancellationToken);
            if (created is null)
            {
                var existingLinks = await _flowRepository.GetFlowSceneLinksAsync(workspaceId, flowId, cancellationToken);
                if (!existingLinks.Any(existing => existing.SceneId == sceneId))
                    return null;
            }

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return BuildFlowSceneContextItem(
                flowId,
                workspaceId,
                scene,
                userId,
                WorkspaceRolePermissions.CanSeeAllContent(membership.Role),
                accessibleGroupIds);
        }

        public async Task<bool> DetachFlowSceneAsync(int flowId, int sceneId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return false;

            await EnsureCanManageFlowAsync(flow.OwnerUserId, cancellationToken);
            return await _flowRepository.DeleteFlowSceneLinkAsync(workspaceId, flowId, sceneId, cancellationToken);
        }

        public async Task<string?> GetFlowSceneContentAsync(int flowId, int sceneId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var flow = await GetFlowByIdAsync(flowId, cancellationToken);
            if (flow is null)
                return null;

            if (!await IsSceneAttachedToFlowAsync(flow.WorkspaceId, flowId, sceneId, cancellationToken))
                return null;

            var scene = await _sceneRepository.GetSceneByIdAsync(flow.WorkspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            return await LoadSceneContentAsync(flow.WorkspaceId, sceneId, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
        }

        private static FlowSceneContextItem BuildFlowSceneContextItem(
            int flowId,
            int workspaceId,
            Scene scene,
            int userId,
            bool canSeeAll,
            IReadOnlySet<int> accessibleGroupIds) =>
            new()
            {
                SceneId = scene.Id,
                FlowId = flowId,
                WorkspaceId = workspaceId,
                Name = scene.Name,
                GroupId = scene.GroupId,
                UsableInFlow = true,
                CanViewDirectly = WorkspaceContentVisibility.IsSceneVisibleToUser(scene, userId, canSeeAll, accessibleGroupIds),
                ResolvedFrom = FlowSceneResolvedFrom.FlowAttachment
            };

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

        public async Task<List<int>> GetFlowGroupIdsAsync(int flowId, CancellationToken cancellationToken)
        {
            var flow = await GetFlowByIdAsync(flowId, cancellationToken);
            if (flow is null)
                throw new KeyNotFoundException("Flow not found.");

            return flow.GroupIds;
        }

        public async Task<List<int>?> SetFlowGroupIdsAsync(int flowId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);

            var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(0, groupIds.ToList());
            await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                _groupRepository,
                _groupAccessResolver,
                workspaceId,
                resolvedGroupIds,
                membership,
                RequireCurrentUserId(),
                cancellationToken);

            await _flowRepository.ReplaceFlowGroupSharesAsync(workspaceId, flowId, resolvedGroupIds, cancellationToken);
            await _flowRepository.SyncFlowPrimaryGroupIdAsync(workspaceId, flowId, cancellationToken);
            return resolvedGroupIds.ToList();
        }

        public async Task<bool> AddFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, groupId, membership, RequireCurrentUserId(), cancellationToken);

            await ContentGroupShareOperations.PopulateFlowGroupIdsAsync(_flowRepository, workspaceId, existing, cancellationToken);
            var added = await _flowRepository.AddFlowGroupShareAsync(workspaceId, flowId, groupId, cancellationToken);
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
    }
}
