using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Content
{
    public static class WorkspaceContentVisibility
    {
        public static bool IsAssetVisibleToUser(
            Asset asset,
            int userId,
            bool canSeeAllContent,
            IReadOnlySet<int> accessibleGroupIds)
        {
            if (canSeeAllContent)
                return true;

            if (asset.OwnerUserId == userId)
                return true;

            return SharesAnyGroup(ResourceGroupPlacement.EffectiveGroupIds(asset.GroupIds, asset.GroupId), accessibleGroupIds);
        }

        public static bool IsSceneVisibleToUser(
            Scene scene,
            int userId,
            bool canSeeAllContent,
            IReadOnlySet<int> accessibleGroupIds)
        {
            if (canSeeAllContent)
                return true;

            if (scene.OwnerUserId == userId)
                return true;

            return SharesAnyGroup(ResourceGroupPlacement.EffectiveGroupIds(scene.GroupIds, scene.GroupId), accessibleGroupIds);
        }

        public static bool IsFlowVisibleToUser(
            Flow flow,
            int userId,
            bool canSeeAllContent,
            IReadOnlySet<int> accessibleGroupIds)
        {
            if (canSeeAllContent)
                return true;

            if (flow.OwnerUserId == userId)
                return true;

            return SharesAnyGroup(ResourceGroupPlacement.EffectiveGroupIds(flow.GroupIds, flow.GroupId), accessibleGroupIds);
        }

        private static bool SharesAnyGroup(IReadOnlyList<int> resourceGroupIds, IReadOnlySet<int> accessibleGroupIds)
        {
            foreach (var groupId in resourceGroupIds)
            {
                if (accessibleGroupIds.Contains(groupId))
                    return true;
            }

            return false;
        }
    }
}
