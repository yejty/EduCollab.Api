using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Content
{
    public static class WorkspaceContentVisibility
    {
        public static HashSet<int> BuildVisibleFolderIds(
            IEnumerable<AssetFolder> folders,
            IEnumerable<AssetFolderGroupShare> shares,
            IReadOnlySet<int> userGroupIds)
        {
            var sharedFolderIds = shares
                .Where(share => userGroupIds.Contains(share.GroupId))
                .Select(share => share.FolderId)
                .ToHashSet();

            if (sharedFolderIds.Count == 0)
                return new HashSet<int>();

            var foldersById = folders.ToDictionary(folder => folder.Id);
            var visibleFolderIds = new HashSet<int>();

            foreach (var folder in foldersById.Values)
            {
                var current = folder;
                while (true)
                {
                    if (sharedFolderIds.Contains(current.Id))
                    {
                        visibleFolderIds.Add(folder.Id);
                        break;
                    }

                    if (current.ParentFolderId is not int parentId || !foldersById.TryGetValue(parentId, out var parent))
                        break;

                    current = parent;
                }
            }

            return visibleFolderIds;
        }

        public static bool IsAssetVisibleToUser(
            Asset asset,
            int userId,
            bool canSeeAllContent,
            IReadOnlySet<int> userGroupIds,
            IReadOnlySet<int> directlySharedAssetIds,
            IReadOnlySet<int> visibleFolderIds)
        {
            if (canSeeAllContent)
                return true;

            if (asset.OwnerUserId == userId)
                return true;

            if (directlySharedAssetIds.Contains(asset.Id))
                return true;

            return asset.FolderId is int folderId && visibleFolderIds.Contains(folderId);
        }

        public static bool IsFolderVisibleToUser(
            AssetFolder folder,
            int userId,
            bool canSeeAllContent,
            IReadOnlySet<int> visibleFolderIds)
        {
            if (canSeeAllContent)
                return true;

            if (folder.CreatedByUserId == userId)
                return true;

            return visibleFolderIds.Contains(folder.Id);
        }

        public static bool IsSceneVisibleToUser(
            Scene scene,
            int userId,
            bool canSeeAllContent,
            IReadOnlySet<int> directlySharedSceneIds)
        {
            if (canSeeAllContent)
                return true;

            if (scene.OwnerUserId == userId)
                return true;

            return directlySharedSceneIds.Contains(scene.Id);
        }
    }
}
