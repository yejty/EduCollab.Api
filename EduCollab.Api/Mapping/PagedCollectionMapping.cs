using EduCollab.Api.Query;
using EduCollab.Application.Models;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Flows;
using EduCollab.Contracts.Responses.Scenes;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Mapping
{
    public static class PagedCollectionMapping
    {
        public static AssetsResponse MapToResponse(this PagedResult<Asset> paged) =>
            new()
            {
                Assets = paged.Items.Select(static asset => asset.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static ScenesResponse MapToResponse(this PagedResult<Scene> paged) =>
            new()
            {
                Scenes = paged.Items.Select(static scene => scene.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static GroupsResponse MapToResponse(this PagedResult<Group> paged) =>
            new()
            {
                Groups = paged.Items.Select(static group => group.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static FlowsResponse MapToResponse(this PagedResult<Flow> paged) =>
            new()
            {
                Flows = paged.Items.Select(static flow => flow.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static WorkspacesResponse MapToResponse(this PagedResult<Workspace> paged) =>
            new()
            {
                Workspaces = paged.Items.Select(static workspace => workspace.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static WorkspaceMembersResponse MapToResponse(this PagedResult<WorkspaceMember> paged) =>
            new()
            {
                Members = paged.Items.Select(static member => member.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static GroupMembersResponse MapToResponse(
            this PagedResult<GroupMember> paged,
            IReadOnlyDictionary<int, string> workspaceRolesByUserId) =>
            new()
            {
                Members = paged.Items
                    .Select(member => member.MapToResponse(
                        workspaceRolesByUserId.TryGetValue(member.UserId, out var role)
                            ? role
                            : string.Empty))
                    .ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static WorkspaceCreationRequestsResponse MapToResponse(this PagedResult<WorkspaceCreationRequest> paged) =>
            new()
            {
                Requests = paged.Items.Select(static request => request.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static SceneAssetsResponse MapToResponse(this PagedResult<SceneAssetContextItem> paged) =>
            new()
            {
                Assets = paged.Items.Select(static item => item.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };

        public static FlowScenesResponse MapToResponse(this PagedResult<FlowSceneContextItem> paged) =>
            new()
            {
                Scenes = paged.Items.Select(static item => item.MapToResponse()).ToList(),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
            };
    }
}
