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

            return accessibleGroupIds.Contains(asset.GroupId);
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

            return accessibleGroupIds.Contains(scene.GroupId);
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

            return accessibleGroupIds.Contains(flow.GroupId);
        }
    }
}
