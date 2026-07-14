namespace EduCollab.Application.Services.Content
{
    public static class ResourceGroupPlacement
    {
        public static IReadOnlyList<int> ResolveGroupIds(IReadOnlyList<int>? groupIds)
        {
            if (groupIds is not { Count: > 0 })
                return Array.Empty<int>();

            if (groupIds.Any(id => id <= 0))
                throw new ArgumentException("Each group id must be a positive integer.", nameof(groupIds));

            return groupIds.Distinct().ToList();
        }

        public static int PrimaryGroupId(IReadOnlyList<int> groupIds) =>
            groupIds.Count > 0 ? groupIds[0] : 0;

        public static IReadOnlyList<int> EffectiveGroupIds(IReadOnlyList<int> groupIds, int legacyGroupId)
        {
            if (groupIds.Count > 0)
                return groupIds;

            return legacyGroupId > 0 ? new List<int> { legacyGroupId } : Array.Empty<int>();
        }
    }
}
