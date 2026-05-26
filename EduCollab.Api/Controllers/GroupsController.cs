using EduCollab.Api.Mapping;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Groups;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        /// <summary>
        /// Create a new group and assign the current user as the admin.
        /// </summary>
        /// <param name="workspaceId">Workspace Id.</param>
        /// <param name="request">Group creation payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Group was created successfully.</response>
        /// <response code="400">Group could not be created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Groups.Create)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateGroup(int workspaceId, [FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var group = request.MapToGroup();

            var created = await _groupService.CreateGroupAsync(workspaceId, group, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Group could not be created."
                });
            }
            var response = group.MapToResponse();
            response.CurrentUserRole = GroupRole.Admin.ToString();
            return CreatedAtAction(nameof(GetGroup), new { workspaceId, groupId = group.Id }, response);
        }

        /// <summary>
        /// Get all groups in workspace.
        /// </summary>
        /// <param name="workspaceId">Workspace Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">List of groups.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAll)]
        [ProducesResponseType(typeof(GroupsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GroupsResponse>> GetAllGroups(int workspaceId, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetAllGroupsAsync(workspaceId, cancellationToken);
            var response = groups.MapToResponse(); 
            return Ok(response);
        }

        /// <summary>
        /// Get group by id in workspace.
        /// </summary>
        /// <param name="workspaceId">Workspace Id.</param>
        ///  <param name="groupId">Group Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group info.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Groups.Get)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GroupResponse>> GetGroup(int groupId, int workspaceId, CancellationToken cancellationToken)
        {
            var group = await _groupService.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
            {
                return NotFound();
            }
            var response = group.MapToResponse();
            var myMembership = await _groupService.GetCurrentUserGroupMemberAsync(workspaceId, groupId, cancellationToken);
            response.CurrentUserRole = myMembership?.Role.ToString();
            return Ok(response);
        }

        /// <summary>
        /// Update group by id in workspace.
        /// </summary>
        /// <param name="workspaceId">Workspace Id.</param>
        /// <param name="request"></param>
        ///  <param name="groupId">Group Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group updated successfully.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Groups.Update)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GroupResponse>> UpdateGroup(int groupId, int workspaceId, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)
        {
            var group = request.MapToGroup(groupId);
            var updatedGroup = await _groupService.UpdateGroupAsync(workspaceId, group, cancellationToken);
            if (updatedGroup is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Group was not found.",
                });
            }
            var response = updatedGroup.MapToResponse();
            var myMembership = await _groupService.GetCurrentUserGroupMemberAsync(workspaceId, groupId, cancellationToken);
            response.CurrentUserRole = myMembership?.Role.ToString();
            return Ok(response);
        }

        /// <summary>
        /// Delete group by id in workspace.
        /// </summary>
        /// <param name="workspaceId">Workspace Id.</param>
        ///  <param name="groupId">Group Id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Group deleted successfully.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Groups.Delete)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteGroup(int groupId, int workspaceId, CancellationToken cancellationToken)
        {
            var deleted = await _groupService.DeleteGroupAsync(workspaceId, groupId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAllMembers)]
        [ProducesResponseType(typeof(GroupMembersResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GroupMembersResponse>> GetAllMembers(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var members = await _groupService.GetAllGroupMembersAsync(workspaceId, groupId, cancellationToken);
            return Ok(members.MapToResponse());
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Groups.CreateMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupMemberResponse>> CreateMember(int workspaceId, int groupId, [FromBody] CreateGroupMemberRequest request, CancellationToken cancellationToken)
        {
            var member = request.MapToGroupMember(groupId);
            var created = await _groupService.CreateGroupMemberAsync(workspaceId, groupId, member, cancellationToken);
            if (created is null)
                return BadRequest(new ErrorResponse { Error = "creation_failed", ErrorDescription = "Group member could not be created." });

            return CreatedAtAction(nameof(GetMember), new { workspaceId, groupId, userId = created.UserId }, created.MapToResponse());
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GroupMemberResponse>> GetMember(int workspaceId, int groupId, int userId, CancellationToken cancellationToken)
        {
            var member = await _groupService.GetGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
            if (member is null)
                return NotFound();

            return Ok(member.MapToResponse());
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Groups.UpdateMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GroupMemberResponse>> UpdateMember(int workspaceId, int groupId, int userId, [FromBody] UpdateGroupMemberRequest request, CancellationToken cancellationToken)
        {
            var updated = await _groupService.UpdateGroupMemberAsync(workspaceId, groupId, userId, request.MapToGroupRole(), cancellationToken);
            if (updated is null)
                return NotFound(new ErrorResponse { Error = "update_failed", ErrorDescription = "Group member was not found." });

            return Ok(updated.MapToResponse());
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Groups.DeleteMember)]
        public async Task<IActionResult> DeleteMember(int workspaceId, int groupId, int userId, CancellationToken cancellationToken)
        {
            var deleted = await _groupService.DeleteGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetFolders)]
        public IActionResult GetFolders(int workspaceId, int groupId)
        {
            throw new NotImplementedException();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetSubFolders)]
        public IActionResult GetSubFolders(int workspaceId, int groupId, int folderId)
        {
            throw new NotImplementedException();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAssetsInFolders)]
        public IActionResult GetAssetsInFolders(int workspaceId, int groupId, int folderId)
        {
            throw new NotImplementedException();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAssets)]
        public IActionResult GetAssets(int workspaceId, int groupId)
        {
            throw new NotImplementedException();
        }
    }
}
