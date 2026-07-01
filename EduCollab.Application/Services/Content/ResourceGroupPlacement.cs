namespace EduCollab.Application.Services.Content
{
    public static class ResourceGroupPlacement
    {
        public static IReadOnlyList<int> ResolveGroupIds(int groupId, IReadOnlyList<int>? groupIds)
        {
            if (groupIds is { Count: > 0 })
                return groupIds.Where(id => id > 0).Distinct().ToList();

            if (groupId > 0)
                return new List<int> { groupId };

            return Array.Empty<int>();
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
