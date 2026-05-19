using EduCollab.Api.Mapping;
using EduCollab.Application.Models.Groups;
using EduCollab.Application.Models.Workspaces;
using EduCollab.Application.Services.Groups;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Workspaces;
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
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, response);
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
                return BadRequest(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Group could not be updated. Ensure you have permission to update this workspace and that the request is valid.",
                });
            }
            var response = updatedGroup.MapToResponse();
            return Ok(response);
        }

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
    }
}
