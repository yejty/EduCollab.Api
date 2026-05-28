using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AdminWorkspacesController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public AdminWorkspacesController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        /// <summary>
        /// List all workspaces in the platform.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">All workspaces.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetAll)]
        [ProducesResponseType(typeof(WorkspacesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WorkspacesResponse>> GetAllWorkspaces(CancellationToken cancellationToken)
        {
            var workspaces = await _workspaceService.GetWorkspacesAsync(cancellationToken);
            return Ok(workspaces.MapToResponse());
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

            return Ok(workspace.MapToResponse());
        }

        /// <summary>
        /// List members of a selected workspace.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Members in the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="404">Workspace not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetAllMembers)]
        [ProducesResponseType(typeof(WorkspaceMembersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceMembersResponse>> GetWorkspaceMembers(int id, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceService.GetWorkspaceAsync(id, cancellationToken);
            if (workspace is null)
            {
                return NotFound();
            }

            var members = await _workspaceService.GetWorkspaceMembersAsync(id, cancellationToken);
            return Ok(members.MapToResponse());
        }
    }
}
