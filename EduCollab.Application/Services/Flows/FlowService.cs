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
        Task<bool> CreateFlowAsync(Flow flow, CancellationToken cancellationToken);
        Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsInGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken);
        Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken);
        Task<Flow?> UpdateFlowAsync(Flow flow, CancellationToken cancellationToken);
        Task<bool> DeleteFlowAsync(int flowId, CancellationToken cancellationToken);
        Task<List<FlowScene>> GetFlowScenesAsync(int flowId, CancellationToken cancellationToken);
        Task<FlowScene?> AddFlowSceneAsync(int flowId, int sceneId, int sortOrder, CancellationToken cancellationToken);
        Task<bool> RemoveFlowSceneAsync(int flowId, int sceneId, CancellationToken cancellationToken);
        Task<bool> CanCurrentUserManageFlowAsync(int ownerUserId, CancellationToken cancellationToken);
    }

    public class FlowService : IFlowService
    {
        private readonly IFlowRepository _flowRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupAccessResolver _groupAccessResolver;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public FlowService(
            IFlowRepository flowRepository,
            IGroupRepository groupRepository,
            IGroupAccessResolver groupAccessResolver,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _flowRepository = flowRepository;
            _groupRepository = groupRepository;
            _groupAccessResolver = groupAccessResolver;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
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

        public async Task<bool> CreateFlowAsync(Flow flow, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(flow);

            if (flow.GroupId <= 0)
                throw new ArgumentException("GroupId is required.", nameof(flow.GroupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
                throw new AccessDeniedException("Viewers have read-only access to flows.");

            var userId = RequireCurrentUserId();
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, flow.GroupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, flow.GroupId, membership, userId, cancellationToken);

            flow.WorkspaceId = workspaceId;
            flow.OwnerUserId = userId;
            flow.Name = RequireTrimmed(flow.Name, nameof(flow.Name));
            flow.Description = string.IsNullOrWhiteSpace(flow.Description) ? null : flow.Description.Trim();
            flow.CreatedAtUtc = DateTime.UtcNow;
            flow.UpdatedAtUtc = flow.CreatedAtUtc;

            var id = await _flowRepository.CreateFlowAsync(workspaceId, flow, cancellationToken);
            if (id <= 0)
                return false;

            flow.Id = id;
            return true;
        }

        public async Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var flows = await _flowRepository.GetAllFlowsAsync(workspaceId, cancellationToken);
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return flows
                .Where(flow => WorkspaceContentVisibility.IsFlowVisibleToUser(flow, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds))
                .ToList();
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
            return await _flowRepository.GetFlowsByGroupAsync(workspaceId, groupId, cancellationToken);
        }

        public async Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            return await _flowRepository.GetFlowsByOwnerAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return null;

            var userId = RequireCurrentUserId();
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return WorkspaceContentVisibility.IsFlowVisibleToUser(flow, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds)
                ? flow
                : null;
        }

        public async Task<Flow?> UpdateFlowAsync(Flow flow, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(flow);
            if (flow.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(flow.Id));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _flowRepository.GetFlowByIdAsync(workspaceId, flow.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageFlowAsync(existing.OwnerUserId, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, flow.GroupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, flow.GroupId, membership, RequireCurrentUserId(), cancellationToken);

            existing.Name = RequireTrimmed(flow.Name, nameof(flow.Name));
            existing.Description = string.IsNullOrWhiteSpace(flow.Description) ? null : flow.Description.Trim();
            existing.GroupId = flow.GroupId;

            return await _flowRepository.UpdateFlowAsync(workspaceId, existing, cancellationToken);
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

        public async Task<List<FlowScene>> GetFlowScenesAsync(int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));

            var flow = await GetFlowByIdAsync(flowId, cancellationToken)
                ?? throw new KeyNotFoundException("Flow not found.");

            return await _flowRepository.GetFlowScenesAsync(flow.WorkspaceId, flowId, cancellationToken);
        }

        public async Task<FlowScene?> AddFlowSceneAsync(int flowId, int sceneId, int sortOrder, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(flowId));
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var flow = await _flowRepository.GetFlowByIdAsync(workspaceId, flowId, cancellationToken);
            if (flow is null)
                return null;

            await EnsureCanManageFlowAsync(flow.OwnerUserId, cancellationToken);

            return await _flowRepository.AddFlowSceneAsync(workspaceId, new FlowScene
            {
                FlowId = flowId,
                SceneId = sceneId,
                SortOrder = sortOrder
            }, cancellationToken);
        }

        public async Task<bool> RemoveFlowSceneAsync(int flowId, int sceneId, CancellationToken cancellationToken)
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
            return await _flowRepository.RemoveFlowSceneAsync(workspaceId, flowId, sceneId, cancellationToken);
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
    }
}
