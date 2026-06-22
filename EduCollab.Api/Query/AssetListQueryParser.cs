namespace EduCollab.Api.Query
{
    public readonly record struct AssetListFilter(bool OwnerIsCurrentUser, int? FolderId, int? GroupId);

    public static class AssetListQueryParser
    {
        public static bool TryParse(
            string? owner,
            int? folderId,
            int? groupId,
            out AssetListFilter filter,
            out string? errorDetail)
        {
            filter = default;

            if (!OwnerQueryParser.TryParse(owner, out var ownerIsCurrentUser, out errorDetail))
                return false;

            if (folderId is <= 0)
            {
                errorDetail = "folderId must be a positive integer when specified.";
                return false;
            }

            if (groupId is <= 0)
            {
                errorDetail = "groupId must be a positive integer when specified.";
                return false;
            }

            if (ownerIsCurrentUser && (folderId is not null || groupId is not null))
            {
                errorDetail = "owner cannot be combined with folderId or groupId.";
                return false;
            }

            filter = new AssetListFilter(ownerIsCurrentUser, folderId, groupId);
            errorDetail = null;
            return true;
        }
    }
}
