using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Application.Services.Content
{
    internal static class ContentGroupShareOperations
    {
        internal static async Task PopulateAssetGroupIdsAsync(
            IAssetRepository repository,
            int workspaceId,
            Asset asset,
            CancellationToken cancellationToken)
        {
            asset.GroupIds = await repository.GetAssetGroupIdsAsync(workspaceId, asset.Id, cancellationToken);
            asset.GroupId = ResourceGroupPlacement.PrimaryGroupId(asset.GroupIds);
        }

        internal static async Task PopulateAssetGroupIdsAsync(
            IAssetRepository repository,
            int workspaceId,
            List<Asset> assets,
            CancellationToken cancellationToken)
        {
            if (assets.Count == 0)
                return;

            var groupIdsByAssetId = await repository.GetAssetGroupIdsByAssetIdsAsync(
                workspaceId,
                assets.Select(asset => asset.Id).ToArray(),
                cancellationToken);

            foreach (var asset in assets)
            {
                asset.GroupIds = groupIdsByAssetId.TryGetValue(asset.Id, out var groupIds)
                    ? groupIds
                    : new List<int>();
                asset.GroupId = ResourceGroupPlacement.PrimaryGroupId(asset.GroupIds);
            }
        }

        internal static async Task PopulateSceneGroupIdsAsync(
            ISceneRepository repository,
            int workspaceId,
            Scene scene,
            CancellationToken cancellationToken)
        {
            scene.GroupIds = await repository.GetSceneGroupIdsAsync(workspaceId, scene.Id, cancellationToken);
            scene.GroupId = ResourceGroupPlacement.PrimaryGroupId(scene.GroupIds);
        }

        internal static async Task PopulateSceneGroupIdsAsync(
            ISceneRepository repository,
            int workspaceId,
            List<Scene> scenes,
            CancellationToken cancellationToken)
        {
            if (scenes.Count == 0)
                return;

            var groupIdsBySceneId = await repository.GetSceneGroupIdsBySceneIdsAsync(
                workspaceId,
                scenes.Select(scene => scene.Id).ToArray(),
                cancellationToken);

            foreach (var scene in scenes)
            {
                scene.GroupIds = groupIdsBySceneId.TryGetValue(scene.Id, out var groupIds)
                    ? groupIds
                    : new List<int>();
                scene.GroupId = ResourceGroupPlacement.PrimaryGroupId(scene.GroupIds);
            }
        }

        internal static async Task PopulateFlowGroupIdsAsync(
            IFlowRepository repository,
            int workspaceId,
            Flow flow,
            CancellationToken cancellationToken)
        {
            flow.GroupIds = await repository.GetFlowGroupIdsAsync(workspaceId, flow.Id, cancellationToken);
            flow.GroupId = ResourceGroupPlacement.PrimaryGroupId(flow.GroupIds);
        }

        internal static async Task PopulateFlowGroupIdsAsync(
            IFlowRepository repository,
            int workspaceId,
            List<Flow> flows,
            CancellationToken cancellationToken)
        {
            if (flows.Count == 0)
                return;

            var groupIdsByFlowId = await repository.GetFlowGroupIdsByFlowIdsAsync(
                workspaceId,
                flows.Select(flow => flow.Id).ToArray(),
                cancellationToken);

            foreach (var flow in flows)
            {
                flow.GroupIds = groupIdsByFlowId.TryGetValue(flow.Id, out var groupIds)
                    ? groupIds
                    : new List<int>();
                flow.GroupId = ResourceGroupPlacement.PrimaryGroupId(flow.GroupIds);
            }
        }

        internal static async Task EnsureCanPlaceInGroupsAsync(
            IGroupRepository groupRepository,
            IGroupAccessResolver groupAccessResolver,
            int workspaceId,
            IReadOnlyList<int> groupIds,
            WorkspaceMember membership,
            int userId,
            CancellationToken cancellationToken)
        {
            foreach (var groupId in groupIds)
            {
                var group = await groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
                if (group is null)
                    throw new KeyNotFoundException("Group not found.");

                if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                    continue;

                if (await groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))
                    continue;

                throw new AccessDeniedException("You do not have access to place resources in this group.");
            }
        }

        internal static bool ManagerCanManageViaGroups(
            WorkspaceMember membership,
            int ownerUserId,
            int userId,
            IReadOnlyList<int> resourceGroupIds,
            int legacyGroupId,
            IReadOnlySet<int> accessibleGroupIds)
        {
            if (membership.Role != WorkspaceRole.Manager)
                return false;

            if (ownerUserId == userId)
                return true;

            var effectiveGroupIds = ResourceGroupPlacement.EffectiveGroupIds(resourceGroupIds, legacyGroupId);
            foreach (var groupId in effectiveGroupIds)
            {
                if (accessibleGroupIds.Contains(groupId))
                    return true;
            }

            return false;
        }
    }
}
