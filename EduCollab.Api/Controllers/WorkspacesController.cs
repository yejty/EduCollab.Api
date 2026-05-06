using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Responses;
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
        /// List users that belong to a workspace (with membership role).
        /// </summary>
        /// <param name="workspaceId">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Users in the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this workspace.</response>
        /// <response code="404">Workspace not found.</response>
        [Authorize]
        [HttpGet]
        [Route("api/workspaces/{workspaceId:long}/users")]
        [ProducesResponseType(typeof(WorkspaceUsersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceUsersResponse>> GetWorkspaceUsers(long workspaceId, CancellationToken cancellationToken)
        {
            var result = await _workspaceService.GetWorkspaceUsersAsync(workspaceId, cancellationToken);
            return Ok(result);
        }
    }
}
