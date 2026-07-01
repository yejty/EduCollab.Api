using EduCollab.Application.Models;

namespace EduCollab.Api.Query
{
    public static class ResourceSortProfiles
    {
        public static class NamedResource
        {
            public static readonly IReadOnlyCollection<string> AllowedFields =
                ["name", "createdAt", "updatedAt", "id"];

            public static readonly SortSpecification Default =
                new() { Field = "name", Direction = SortDirection.Asc };

            public static List<Asset> ApplyAssets(IEnumerable<Asset> items, SortSpecification sort) =>
                SortApplier.Apply(items, sort, AssetSelectors, static x => x.Id);

            public static List<Scene> ApplyScenes(IEnumerable<Scene> items, SortSpecification sort) =>
                SortApplier.Apply(items, sort, SceneSelectors, static x => x.Id);

            public static List<Group> ApplyGroups(IEnumerable<Group> items, SortSpecification sort) =>
                SortApplier.Apply(items, sort, GroupSelectors, static x => x.Id);

            public static List<Flow> ApplyFlows(IEnumerable<Flow> items, SortSpecification sort) =>
                SortApplier.Apply(items, sort, FlowSelectors, static x => x.Id);

            public static List<Workspace> ApplyWorkspaces(IEnumerable<Workspace> items, SortSpecification sort) =>
                SortApplier.Apply(items, sort, WorkspaceSelectors, static x => x.Id);

            private static readonly Dictionary<string, Func<Asset, object>> AssetSelectors = new(StringComparer.Ordinal)
            {
                ["name"] = static x => x.Name,
                ["createdAt"] = static x => x.CreatedAtUtc,
                ["updatedAt"] = static x => x.UpdatedAtUtc,
                ["id"] = static x => x.Id,
            };

            private static readonly Dictionary<string, Func<Scene, object>> SceneSelectors = new(StringComparer.Ordinal)
            {
                ["name"] = static x => x.Name,
                ["createdAt"] = static x => x.CreatedAtUtc,
                ["updatedAt"] = static x => x.UpdatedAtUtc,
                ["id"] = static x => x.Id,
            };

            private static readonly Dictionary<string, Func<Group, object>> GroupSelectors = new(StringComparer.Ordinal)
            {
                ["name"] = static x => x.Name,
                ["createdAt"] = static x => x.CreatedAtUtc,
                ["updatedAt"] = static x => x.UpdatedAtUtc,
                ["id"] = static x => x.Id,
            };

            private static readonly Dictionary<string, Func<Flow, object>> FlowSelectors = new(StringComparer.Ordinal)
            {
                ["name"] = static x => x.Name,
                ["createdAt"] = static x => x.CreatedAtUtc,
                ["updatedAt"] = static x => x.UpdatedAtUtc,
                ["id"] = static x => x.Id,
            };

            private static readonly Dictionary<string, Func<Workspace, object>> WorkspaceSelectors = new(StringComparer.Ordinal)
            {
                ["name"] = static x => x.Name,
                ["createdAt"] = static x => x.CreatedAtUtc,
                ["updatedAt"] = static x => x.UpdatedAtUtc,
                ["id"] = static x => x.Id,
            };
        }

        public static class WorkspaceMember
        {
            public static readonly IReadOnlyCollection<string> AllowedFields =
                ["userId", "joinedAt", "role"];

            public static readonly SortSpecification Default =
                new() { Field = "joinedAt", Direction = SortDirection.Asc };

            public static List<Application.Models.WorkspaceMember> Apply(
                IEnumerable<Application.Models.WorkspaceMember> items,
                SortSpecification sort) =>
                SortApplier.Apply(items, sort, Selectors, static x => x.UserId);

            private static readonly Dictionary<string, Func<Application.Models.WorkspaceMember, object>> Selectors =
                new(StringComparer.Ordinal)
                {
                    ["userId"] = static x => x.UserId,
                    ["joinedAt"] = static x => x.JoinedAtUtc,
                    ["role"] = static x => x.Role.ToString(),
                };
        }

        public static class GroupMember
        {
            public static readonly IReadOnlyCollection<string> AllowedFields =
                ["userId", "joinedAt"];

            public static readonly SortSpecification Default =
                new() { Field = "joinedAt", Direction = SortDirection.Asc };

            public static List<Application.Models.GroupMember> Apply(
                IEnumerable<Application.Models.GroupMember> items,
                SortSpecification sort) =>
                SortApplier.Apply(items, sort, Selectors, static x => x.UserId);

            private static readonly Dictionary<string, Func<Application.Models.GroupMember, object>> Selectors =
                new(StringComparer.Ordinal)
                {
                    ["userId"] = static x => x.UserId,
                    ["joinedAt"] = static x => x.JoinedAtUtc,
                };
        }

        public static class SceneAsset
        {
            public static readonly IReadOnlyCollection<string> AllowedFields =
                ["name", "assetId"];

            public static readonly SortSpecification Default =
                new() { Field = "name", Direction = SortDirection.Asc };

            public static List<SceneAssetContextItem> Apply(
                IEnumerable<SceneAssetContextItem> items,
                SortSpecification sort) =>
                SortApplier.Apply(items, sort, Selectors, static x => x.AssetId);

            private static readonly Dictionary<string, Func<SceneAssetContextItem, object>> Selectors =
                new(StringComparer.Ordinal)
                {
                    ["name"] = static x => x.Name,
                    ["assetId"] = static x => x.AssetId,
                };
        }

        public static class FlowScene
        {
            public static readonly IReadOnlyCollection<string> AllowedFields =
                ["name", "sceneId"];

            public static readonly SortSpecification Default =
                new() { Field = "name", Direction = SortDirection.Asc };

            public static List<FlowSceneContextItem> Apply(
                IEnumerable<FlowSceneContextItem> items,
                SortSpecification sort) =>
                SortApplier.Apply(items, sort, Selectors, static x => x.SceneId);

            private static readonly Dictionary<string, Func<FlowSceneContextItem, object>> Selectors =
                new(StringComparer.Ordinal)
                {
                    ["name"] = static x => x.Name,
                    ["sceneId"] = static x => x.SceneId,
                };
        }

        public static class WorkspaceCreationRequest
        {
            public static readonly IReadOnlyCollection<string> AllowedFields =
                ["name", "createdAt", "status", "id"];

            public static readonly SortSpecification Default =
                new() { Field = "createdAt", Direction = SortDirection.Desc };

            public static List<Application.Models.WorkspaceCreationRequest> Apply(
                IEnumerable<Application.Models.WorkspaceCreationRequest> items,
                SortSpecification sort) =>
                SortApplier.Apply(items, sort, Selectors, static x => x.Id);

            private static readonly Dictionary<string, Func<Application.Models.WorkspaceCreationRequest, object>> Selectors =
                new(StringComparer.Ordinal)
                {
                    ["name"] = static x => x.Name,
                    ["createdAt"] = static x => x.CreatedAtUtc,
                    ["status"] = static x => x.Status.ToString(),
                    ["id"] = static x => x.Id,
                };
        }
    }
}
