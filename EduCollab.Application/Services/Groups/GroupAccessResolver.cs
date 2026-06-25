using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Groups
{
    public interface IGroupAccessResolver
    {
        Task<HashSet<int>> GetEffectiveAccessibleGroupIdsAsync(int workspaceId, int userId, CancellationToken cancellationToken);
        Task<bool> HasEffectiveAccessAsync(int workspaceId, int userId, int groupId, CancellationToken cancellationToken);
    }

    public sealed class GroupAccessResolver : IGroupAccessResolver
    {
        private readonly IGroupRepository _groupRepository;

        public GroupAccessResolver(IGroupRepository groupRepository)
        {
            _groupRepository = groupRepository;
        }

        public async Task<HashSet<int>> GetEffectiveAccessibleGroupIdsAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            var directGroupIds = await _groupRepository.GetUserGroupIdsAsync(workspaceId, userId, cancellationToken);
            if (directGroupIds.Count == 0)
                return [];

            var allGroups = await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);
            return ExpandToDescendants(directGroupIds, allGroups);
        }

        public async Task<bool> HasEffectiveAccessAsync(int workspaceId, int userId, int groupId, CancellationToken cancellationToken)
        {
            var accessible = await GetEffectiveAccessibleGroupIdsAsync(workspaceId, userId, cancellationToken);
            return accessible.Contains(groupId);
        }

        public static HashSet<int> ExpandToDescendants(IReadOnlyList<int> rootGroupIds, IReadOnlyList<Group> allGroups)
        {
            var childrenByParent = allGroups
                .Where(g => g.ParentGroupId is not null)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(child => child.Id).ToList());

            var result = new HashSet<int>();
            var queue = new Queue<int>(rootGroupIds);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!result.Add(current))
                    continue;

                if (childrenByParent.TryGetValue(current, out var children))
                {
                    foreach (var childId in children)
                        queue.Enqueue(childId);
                }
            }

            return result;
        }
    }
}
