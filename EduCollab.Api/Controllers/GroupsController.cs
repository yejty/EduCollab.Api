using EduCollab.Api.Mapping;

using EduCollab.Api.Query;

using EduCollab.Application.Services.Assets;

using EduCollab.Application.Services.Flows;

using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Workspaces;

using EduCollab.Application.Services.Scenes;

using EduCollab.Contracts.Requests.Assets;

using EduCollab.Contracts.Requests.Groups;

using EduCollab.Contracts.Responses;

using EduCollab.Contracts.Responses.Assets;

using EduCollab.Contracts.Responses.Flows;

using EduCollab.Contracts.Responses.Groups;

using EduCollab.Contracts.Responses.Scenes;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace EduCollab.Api.Controllers

{

    [ApiController]

    public class GroupsController : ApiControllerBase

    {

        private readonly IGroupService _groupService;

        private readonly IAssetService _assetService;

        private readonly ISceneService _sceneService;

        private readonly IFlowService _flowService;

        private readonly IWorkspaceService _workspaceService;



        public GroupsController(

            IGroupService groupService,

            IAssetService assetService,

            ISceneService sceneService,

            IFlowService flowService,

            IWorkspaceService workspaceService)

        {

            _groupService = groupService;

            _assetService = assetService;

            _sceneService = sceneService;

            _flowService = flowService;

            _workspaceService = workspaceService;

        }



        private async Task<string> GetCurrentUserWorkspaceRoleAsync(CancellationToken cancellationToken)

        {

            var role = await _groupService.GetCurrentUserWorkspaceRoleAsync(cancellationToken);

            return role.ToString();

        }



        private async Task<IReadOnlyDictionary<int, string>> GetWorkspaceRolesByUserIdAsync(CancellationToken cancellationToken)

        {

            var members = await _workspaceService.GetCurrentWorkspaceMembersAsync(cancellationToken);

            return members.ToDictionary(m => m.UserId, m => m.Role.ToString());

        }



        [Authorize]

        [HttpPost(ApiEndpoints.Groups.Create)]

        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]

        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)

        {

            var group = request.MapToGroup();

            var created = await _groupService.CreateGroupAsync(group, cancellationToken);

            if (!created)

                return ApiBadRequest("creation_failed", "Group could not be created.");



            var response = group.MapToResponse();

            response.CurrentUserRole = await GetCurrentUserWorkspaceRoleAsync(cancellationToken);

            return CreatedAtAction(nameof(GetGroup), new { groupId = group.Id }, response);

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.GetAll)]

        [ProducesResponseType(typeof(GroupsResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<GroupsResponse>> GetAllGroups(

            [FromQuery] bool accessible,

            [FromQuery] int? parentGroupId,

            [FromQuery] string? sort,

            [FromQuery] int? page,

            [FromQuery] int? pageSize,

            CancellationToken cancellationToken)

        {

            if (!TryParseListQuery(

                    sort,

                    page,

                    pageSize,

                    ResourceSortProfiles.NamedResource.AllowedFields,

                    ResourceSortProfiles.NamedResource.Default,

                    out var sortSpecification,

                    out var paginationSpecification,

                    out var problem))

            {

                return problem!;

            }



            var groups = accessible

                ? await _groupService.GetAccessibleGroupsAsync(parentGroupId, cancellationToken)

                : await _groupService.GetAllGroupsAsync(cancellationToken);



            var sortedGroups = ResourceSortProfiles.NamedResource.ApplyGroups(groups, sortSpecification);

            var pagedGroups = PaginationApplier.Apply(sortedGroups, paginationSpecification);

            return Ok(pagedGroups.MapToResponse());

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.Get)]

        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<GroupResponse>> GetGroup(int groupId, CancellationToken cancellationToken)

        {

            var group = await _groupService.GetGroupByIdAsync(groupId, cancellationToken);

            if (group is null)

                return ApiNotFound();



            var response = group.MapToResponse();

            response.CurrentUserRole = await GetCurrentUserWorkspaceRoleAsync(cancellationToken);

            return Ok(response);

        }



        [Authorize]

        [HttpPut(ApiEndpoints.Groups.Update)]

        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<GroupResponse>> UpdateGroup(int groupId, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)

        {

            var group = request.MapToGroup(groupId);

            var updatedGroup = await _groupService.UpdateGroupAsync(group, cancellationToken);

            if (updatedGroup is null)

                return ApiNotFound("update_failed", "Group was not found.");



            var response = updatedGroup.MapToResponse();

            response.CurrentUserRole = await GetCurrentUserWorkspaceRoleAsync(cancellationToken);

            return Ok(response);

        }



        [Authorize]

        [HttpDelete(ApiEndpoints.Groups.Delete)]

        [ProducesResponseType(StatusCodes.Status204NoContent)]

        public async Task<IActionResult> DeleteGroup(int groupId, CancellationToken cancellationToken)

        {

            var deleted = await _groupService.DeleteGroupAsync(groupId, cancellationToken);

            if (!deleted)

                return ApiNotFound();



            return NoContent();

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.GetAssets)]

        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<AssetsResponse>> GetGroupAssets(

            int groupId,

            [FromQuery] string? sort,

            [FromQuery] int? page,

            [FromQuery] int? pageSize,

            CancellationToken cancellationToken)

        {

            if (!TryParseListQuery(

                    sort,

                    page,

                    pageSize,

                    ResourceSortProfiles.NamedResource.AllowedFields,

                    ResourceSortProfiles.NamedResource.Default,

                    out var sortSpecification,

                    out var paginationSpecification,

                    out var problem))

            {

                return problem!;

            }



            var assets = await _assetService.GetAssetsInGroupAsync(groupId, cancellationToken);

            var sorted = ResourceSortProfiles.NamedResource.ApplyAssets(assets, sortSpecification);

            var paged = PaginationApplier.Apply(sorted, paginationSpecification);

            var response = paged.MapToResponse();



            foreach (var asset in response.Assets)

                asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);



            return Ok(response);

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.GetScenes)]

        [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<ScenesResponse>> GetGroupScenes(

            int groupId,

            [FromQuery] string? sort,

            [FromQuery] int? page,

            [FromQuery] int? pageSize,

            CancellationToken cancellationToken)

        {

            if (!TryParseListQuery(

                    sort,

                    page,

                    pageSize,

                    ResourceSortProfiles.NamedResource.AllowedFields,

                    ResourceSortProfiles.NamedResource.Default,

                    out var sortSpecification,

                    out var paginationSpecification,

                    out var problem))

            {

                return problem!;

            }



            var scenes = await _sceneService.GetScenesInGroupAsync(groupId, cancellationToken);

            var sorted = ResourceSortProfiles.NamedResource.ApplyScenes(scenes, sortSpecification);

            var paged = PaginationApplier.Apply(sorted, paginationSpecification);

            var response = paged.MapToResponse();



            foreach (var scene in response.Scenes)

            {

                scene.CanEdit = await _sceneService.CanCurrentUserManageSceneAsync(scene.OwnerUserId, cancellationToken);

                scene.CanManage = scene.CanEdit;

            }



            return Ok(response);

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.GetFlows)]

        [ProducesResponseType(typeof(FlowsResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<FlowsResponse>> GetGroupFlows(

            int groupId,

            [FromQuery] string? sort,

            [FromQuery] int? page,

            [FromQuery] int? pageSize,

            CancellationToken cancellationToken)

        {

            if (!TryParseListQuery(

                    sort,

                    page,

                    pageSize,

                    ResourceSortProfiles.NamedResource.AllowedFields,

                    ResourceSortProfiles.NamedResource.Default,

                    out var sortSpecification,

                    out var paginationSpecification,

                    out var problem))

            {

                return problem!;

            }



            var flows = await _flowService.GetFlowsInGroupAsync(groupId, cancellationToken);

            var sorted = ResourceSortProfiles.NamedResource.ApplyFlows(flows, sortSpecification);

            var paged = PaginationApplier.Apply(sorted, paginationSpecification);

            var response = paged.MapToResponse();



            foreach (var flow in response.Flows)

                flow.CanManage = await _flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);



            return Ok(response);

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.GetAllMembers)]

        [ProducesResponseType(typeof(GroupMembersResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<GroupMembersResponse>> GetAllMembers(int groupId, [FromQuery] string? sort,

            [FromQuery] int? page,

            [FromQuery] int? pageSize,

            CancellationToken cancellationToken)

        {

            if (!TryParseListQuery(

                    sort,

                    page,

                    pageSize,

                    ResourceSortProfiles.GroupMember.AllowedFields,

                    ResourceSortProfiles.GroupMember.Default,

                    out var sortSpecification,

                    out var paginationSpecification,

                    out var problem))

            {

                return problem!;

            }



            var sortedMembers = ResourceSortProfiles.GroupMember.Apply(

                await _groupService.GetAllGroupMembersAsync(groupId, cancellationToken),

                sortSpecification);

            var pagedMembers = PaginationApplier.Apply(sortedMembers, paginationSpecification);

            var rolesByUserId = await GetWorkspaceRolesByUserIdAsync(cancellationToken);

            return Ok(pagedMembers.MapToResponse(rolesByUserId));

        }



        [Authorize]

        [HttpPost(ApiEndpoints.Groups.CreateMember)]

        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status201Created)]

        public async Task<ActionResult<GroupMemberResponse>> CreateMember(int groupId, [FromBody] CreateGroupMemberRequest request, CancellationToken cancellationToken)

        {

            var member = request.MapToGroupMember(groupId);

            var created = await _groupService.CreateGroupMemberAsync(groupId, member, cancellationToken);

            if (created is null)

                return ApiBadRequest("creation_failed", "Group member could not be created.");



            var workspaceMember = await _workspaceService.GetCurrentWorkspaceMemberAsync(created.UserId, cancellationToken);

            var role = workspaceMember?.Role.ToString() ?? string.Empty;

            return CreatedAtAction(nameof(GetMember), new { groupId, userId = created.UserId }, created.MapToResponse(role));

        }



        [Authorize]

        [HttpGet(ApiEndpoints.Groups.GetMember)]

        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<GroupMemberResponse>> GetMember(int groupId, int userId, CancellationToken cancellationToken)

        {

            var member = await _groupService.GetGroupMemberAsync(groupId, userId, cancellationToken);

            if (member is null)

                return ApiNotFound();



            var workspaceMember = await _workspaceService.GetCurrentWorkspaceMemberAsync(userId, cancellationToken);

            var role = workspaceMember?.Role.ToString() ?? string.Empty;

            return Ok(member.MapToResponse(role));

        }



        [Authorize]

        [HttpDelete(ApiEndpoints.Groups.DeleteMember)]

        [ProducesResponseType(StatusCodes.Status204NoContent)]

        public async Task<IActionResult> DeleteMember(int groupId, int userId, CancellationToken cancellationToken)

        {

            var deleted = await _groupService.DeleteGroupMemberAsync(groupId, userId, cancellationToken);

            if (!deleted)

                return ApiNotFound();



            return NoContent();

        }

    }

}


