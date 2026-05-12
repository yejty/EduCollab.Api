using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;
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
        public async Task<IActionResult> InviteToWorkspace([FromBody] InviteUserRequest inviteUserRequest, CancellationToken cancellationToken)
        {
            //await _userService.InviteAsync(inviteUserRequest.Email, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Creates a new user in the workspace based on the provided token.
        /// </summary>
        /// <param name="createUserRequest">Request body containing the user details.</param>
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
        public async Task<ActionResult<UserResponse>> CreateWorkspaceUser([FromBody] CreateUserRequest request, [FromRoute] string invitationToken, CancellationToken cancellationToken)
        {
            var user = request.MapToUser();
            //await _userService.CreateAsync(user, request.Password, invitationToken, cancellationToken);
            return CreatedAtAction(nameof(GetWorkspaceUser), new { id = user.Id }, user);
        }

        /// <summary>
        /// List users that belong to a workspace (with membership role).
        /// </summary>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Users in the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this workspace.</response>
        /// <response code="404">Workspace not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetMember)]
        [ProducesResponseType(typeof(WorkspaceUsersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> GetWorkspaceUser(int id, int userId, CancellationToken cancellationToken)
        {
            return Ok();
        }

        /// <summary>
        /// List users that belong to a workspace (with membership role).
        /// </summary>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Users in the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this workspace.</response>
        /// <response code="404">Workspace not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetMembers)]
        [ProducesResponseType(typeof(WorkspaceUsersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceUsersResponse>> GetWorkspaceUsers(int id, CancellationToken cancellationToken)
        {
            var result = await _workspaceService.GetWorkspaceUsersAsync(id, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Workspaces.Create)]
        [ProducesResponseType(typeof(WorkspaceUsersResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateWorkspace(CancellationToken cancellationToken)
        {
            //return CreatedAtAction(nameof(GetWorkspace), new { id = id }, result);
            return Ok();
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Workspaces.Get)]
        [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<WorkspaceResponse>> GetWorkspace(int id, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceService.GetWorkspaceAsync(id, cancellationToken);
            if (workspace is null)
            {
                return NotFound();
            }
            var response = workspace.MapToResponse();
            return Ok(response);
        }

    }
}
