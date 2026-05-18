using EduCollab.Api.Mapping;
using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;
using EduCollab.Contracts.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class WorkspacesController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspacesController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        /// <summary>
        /// Invite a new user.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="inviteUserRequest">Invitation payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">User invited successfully.</response>
        /// <response code="400">Invalid invitation attempt. Returns an error message.</response>
        /// <response code="401">User is unauthorized.</response>
        /// <response code="403">User is forbidden from accessing this resource.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Workspaces.Invite)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> InviteToWorkspace(int id, [FromBody] InviteUserRequest inviteUserRequest, CancellationToken cancellationToken)
        {
            await _workspaceService.InviteUserToWorkspaceAsync(id, inviteUserRequest.Email, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Creates a new user in the workspace based on the provided token.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="request">Request body containing the user details.</param>
        /// <param name="invitationToken">Invitation token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">User created.</response>
        /// <response code="400">Invalid invitation attempt. Returns an error message.</response>
        /// <response code="401">User is unauthorized.</response>
        /// <response code="403">User is forbidden from accessing this resource.</response>
        [HttpPost(ApiEndpoints.Workspaces.Accept)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<WorkspaceMemberResponse>> CreateWorkspaceUser([FromRoute] int id, [FromBody] RegisterUserRequest request, [FromRoute] string invitationToken, CancellationToken cancellationToken)
        {
            var user = request.MapToUser();
            var created = await _workspaceService.CreateUserInWorkspaceAsync(id, user, request.Password, invitationToken, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "The invitation is invalid, expired, or the user could not be created.",
                });
            }

            var member = await _workspaceService.GetWorkspaceMemberAsync(id, user.Id, cancellationToken);
            if (member is null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "User was created but workspace membership could not be loaded.",
                });
            }

            return CreatedAtAction(nameof(GetWorkspaceUser), new { id, userId = user.Id }, member.MapToResponse());
        }

        /// <summary>
        /// Get one workspace member (membership projection: role, groups, etc.).
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="userId">Workspace member user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the workspace member.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this workspace member.</response>
        /// <response code="404">Workspace or member was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetMember)]
        [ProducesResponseType(typeof(WorkspaceMemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceMemberResponse>> GetWorkspaceUser(int id, int userId, CancellationToken cancellationToken)
        {
            var member = await _workspaceService.GetWorkspaceMemberAsync(id, userId, cancellationToken);
            if (member is null)
                return NotFound();
            return Ok(member.MapToResponse());
        }

        /// <summary>
        /// List members of a workspace (profile summary plus role, groups, join metadata).
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Members in the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this workspace.</response>
        /// <response code="404">Workspace not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetMembers)]
        [ProducesResponseType(typeof(WorkspaceMembersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceMembersResponse>> GetWorkspaceMembers(int id, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceService.GetWorkspaceAsync(id, cancellationToken);
            if (workspace is null)
                return NotFound();

            var members = await _workspaceService.GetWorkspaceMembersAsync(id, cancellationToken);
            var response = members.MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Create a new workspace and assign the current user as the owner.
        /// </summary>
        /// <param name="request">Workspace creation payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Workspace was created successfully.</response>
        /// <response code="400">Workspace could not be created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Workspaces.Create)]
        [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
        {
            var workspace = request.MapToWorkspace();

            var created = await _workspaceService.CreateWorkspaceAsync(workspace, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Workspace could not be created."
                });

            }
            var response = workspace.MapToResponse();
            response.CurrentUserRole = WorkspaceRole.Owner;
            return CreatedAtAction(nameof(GetWorkspace), new { id = workspace.Id }, response);
        }

        /// <summary>
        /// Retrieve a workspace by id.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="404">Workspace was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.Get)]
        [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceResponse>> GetWorkspace(int id, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceService.GetWorkspaceAsync(id, cancellationToken);
            if (workspace is null)
            {
                return NotFound();
            }
            var response = workspace.MapToResponse();
            var myMembership = await _workspaceService.GetCurrentUserWorkspaceMemberAsync(id, cancellationToken);
            response.CurrentUserRole = myMembership?.Role;

            return Ok(response);
        }

        /// <summary>
        /// Update workspace metadata.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="request">Workspace update payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Workspace was updated successfully.</response>
        /// <response code="400">Workspace could not be updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not allowed to update the workspace.</response>
        /// <response code="404">Workspace was not found.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Workspaces.Update)]
        [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkspace(int id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
        {
            var workspace = request.MapToWorkspace(id);
            var updated = await _workspaceService.UpdateWorkspaceAsync(workspace, cancellationToken);
            if (!updated)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Workspace could not be updated. Ensure you have permission to update this workspace and that the request is valid.",
                });
            }
            var response = workspace.MapToResponse();
            return Ok(response);
        }

        /// <summary>
        /// Soft-delete a workspace by archiving it and disconnecting all members from it.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Workspace was archived successfully.</response>
        /// <response code="400">Delete request was invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not allowed to delete the workspace.</response>
        /// <response code="404">Workspace was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Workspaces.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkspace(int id, CancellationToken cancellationToken)
        {
            var deleted = await _workspaceService.DeleteWorkspaceAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Removes a member from the workspace, or the caller leaves (when <paramref name="userId"/> is self).
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="userId">Member user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Member was removed successfully.</response>
        /// <response code="400">Delete-member request was invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not allowed to remove this member.</response>
        /// <response code="404">Workspace or member was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Workspaces.DeleteMember)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkspaceMember(int id, int userId, CancellationToken cancellationToken)
        {
            await _workspaceService.RemoveWorkspaceMemberAsync(id, userId, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Update a workspace member role.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="userId">Member user identifier.</param>
        /// <param name="request">Workspace member update payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Workspace member was updated successfully.</response>
        /// <response code="400">Workspace member could not be updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not allowed to update this member.</response>
        /// <response code="404">Workspace or member was not found.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Workspaces.UpdateMember)]
        [ProducesResponseType(typeof(WorkspaceMemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceMemberResponse>> UpdateWorkspaceMember(int id, int userId, [FromBody] UpdateWorkspaceMemberRequest request, CancellationToken cancellationToken)
        {
            var member = request.MapToWorkspaceMember(id);
            var updatedMember = await _workspaceService.UpdateWorkspaceMemberAsync(id, userId, member, cancellationToken);
            if (updatedMember is null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Workspace member could not be updated. Ensure you have permission to update this member and that the request is valid.",
                });
            }
            var response = updatedMember.MapToResponse();
            return Ok(response);
        }
    }
}