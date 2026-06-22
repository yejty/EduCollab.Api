namespace EduCollab.Api.Query
{
    public readonly record struct AssetFolderListFilter(int? GroupId, int? ParentFolderId);

    public static class AssetFolderListQueryParser
    {
        public static bool TryParse(
            int? groupId,
            int? parentFolderId,
            out AssetFolderListFilter filter,
            out string? errorDetail)
        {
            filter = default;

            if (groupId is <= 0)
            {
                errorDetail = "groupId must be a positive integer when specified.";
                return false;
            }

            if (parentFolderId is <= 0)
            {
                errorDetail = "parentFolderId must be a positive integer when specified.";
                return false;
            }

            filter = new AssetFolderListFilter(groupId, parentFolderId);
            errorDetail = null;
            return true;
        }
    }
}
