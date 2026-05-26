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
        /// Create a group in a workspace.
        /// </summary>
        /// <remarks>
        /// The caller must be a member of the workspace. The creator is automatically added
        /// to the group as <c>Admin</c>.
        ///
        /// Sample request:
        ///
        ///     POST /api/workspaces/1/groups
        ///     {
        ///       "name": "Design Team",
        ///       "description": "People working on UI and UX"
        ///     }
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="request">Group creation payload. Only <c>name</c> and <c>description</c> are client-provided.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Group was created successfully.</response>
        /// <response code="400">Group could not be created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a member of the workspace.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Groups.Create)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
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
        /// List groups in a workspace.
        /// </summary>
        /// <remarks>
        /// The caller must be a member of the workspace. Workspace <c>Owner</c> and
        /// <c>Admin</c> can see all groups. Regular workspace members can call this endpoint,
        /// but group-level endpoints still require group membership.
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">List of groups.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a member of the workspace.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAll)]
        [ProducesResponseType(typeof(GroupsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GroupsResponse>> GetAllGroups(int workspaceId, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetAllGroupsAsync(workspaceId, cancellationToken);
            var response = groups.MapToResponse(); 
            return Ok(response);
        }

        /// <summary>
        /// Get a group by id.
        /// </summary>
        /// <remarks>
        /// The caller must belong to the workspace and either belong to the group or be a
        /// workspace <c>Owner</c>/<c>Admin</c>. The response includes <c>currentUserRole</c>
        /// when the caller is a direct group member.
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group info.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this group.</response>
        /// <response code="404">Group was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Groups.Get)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
        /// Update a group.
        /// </summary>
        /// <remarks>
        /// The caller must be a workspace <c>Owner</c>/<c>Admin</c> or a group <c>Admin</c>.
        /// Fields are partial: omit <c>name</c> or <c>description</c> to keep the current value.
        ///
        /// Sample request:
        ///
        ///     PUT /api/workspaces/1/groups/5
        ///     {
        ///       "name": "Updated Design Team",
        ///       "description": "Updated description"
        ///     }
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="request">Group update payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group updated successfully.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this group.</response>
        /// <response code="404">Group was not found.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Groups.Update)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
        /// Delete a group.
        /// </summary>
        /// <remarks>
        /// The caller must be a workspace <c>Owner</c>/<c>Admin</c> or a group <c>Admin</c>.
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Group deleted successfully.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot delete this group.</response>
        /// <response code="404">Group was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Groups.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteGroup(int groupId, int workspaceId, CancellationToken cancellationToken)
        {
            var deleted = await _groupService.DeleteGroupAsync(workspaceId, groupId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// List members of a group.
        /// </summary>
        /// <remarks>
        /// The caller must belong to the workspace and either belong to the group or be a
        /// workspace <c>Owner</c>/<c>Admin</c>.
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group members.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this group.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAllMembers)]
        [ProducesResponseType(typeof(GroupMembersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GroupMembersResponse>> GetAllMembers(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            var members = await _groupService.GetAllGroupMembersAsync(workspaceId, groupId, cancellationToken);
            return Ok(members.MapToResponse());
        }

        /// <summary>
        /// Add a workspace user to a group.
        /// </summary>
        /// <remarks>
        /// The target user must already be a workspace member. The caller must be a workspace
        /// <c>Owner</c>/<c>Admin</c> or a group <c>Admin</c>.
        ///
        /// Valid role values: <c>Admin</c>, <c>Contributor</c>, <c>Viewer</c>.
        ///
        /// Sample request:
        ///
        ///     POST /api/workspaces/1/groups/5/users
        ///     {
        ///       "userId": 22,
        ///       "role": "Contributor"
        ///     }
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="request">Member creation payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Group member was created.</response>
        /// <response code="400">Request was invalid or user is not a workspace member.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot manage this group.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Groups.CreateMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GroupMemberResponse>> CreateMember(int workspaceId, int groupId, [FromBody] CreateGroupMemberRequest request, CancellationToken cancellationToken)
        {
            var member = request.MapToGroupMember(groupId);
            var created = await _groupService.CreateGroupMemberAsync(workspaceId, groupId, member, cancellationToken);
            if (created is null)
                return BadRequest(new ErrorResponse { Error = "creation_failed", ErrorDescription = "Group member could not be created." });

            return CreatedAtAction(nameof(GetMember), new { workspaceId, groupId, userId = created.UserId }, created.MapToResponse());
        }

        /// <summary>
        /// Get one group member.
        /// </summary>
        /// <remarks>
        /// The caller must belong to the workspace and either belong to the group or be a
        /// workspace <c>Owner</c>/<c>Admin</c>.
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group member info.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this group.</response>
        /// <response code="404">Group member was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupMemberResponse>> GetMember(int workspaceId, int groupId, int userId, CancellationToken cancellationToken)
        {
            var member = await _groupService.GetGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
            if (member is null)
                return NotFound();

            return Ok(member.MapToResponse());
        }

        /// <summary>
        /// Update a group member role.
        /// </summary>
        /// <remarks>
        /// The caller must be a workspace <c>Owner</c>/<c>Admin</c> or a group <c>Admin</c>.
        /// Valid role values: <c>Admin</c>, <c>Contributor</c>, <c>Viewer</c>.
        ///
        /// Sample request:
        ///
        ///     PUT /api/workspaces/1/groups/5/users/22
        ///     {
        ///       "role": "Viewer"
        ///     }
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="request">Role update payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Group member was updated.</response>
        /// <response code="400">Role value was invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot manage this group.</response>
        /// <response code="404">Group member was not found.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Groups.UpdateMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupMemberResponse>> UpdateMember(int workspaceId, int groupId, int userId, [FromBody] UpdateGroupMemberRequest request, CancellationToken cancellationToken)
        {
            var updated = await _groupService.UpdateGroupMemberAsync(workspaceId, groupId, userId, request.MapToGroupRole(), cancellationToken);
            if (updated is null)
                return NotFound(new ErrorResponse { Error = "update_failed", ErrorDescription = "Group member was not found." });

            return Ok(updated.MapToResponse());
        }

        /// <summary>
        /// Remove a user from a group.
        /// </summary>
        /// <remarks>
        /// Workspace <c>Owner</c>/<c>Admin</c> and group <c>Admin</c> can remove users.
        /// A normal group member can remove themselves.
        /// </remarks>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Group member was removed.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot remove this group member.</response>
        /// <response code="404">Group member was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Groups.DeleteMember)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
