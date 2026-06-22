using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AdminWorkspacesController : ApiControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly IPlatformAdminAuthorization _platformAdminAuthorization;

        public AdminWorkspacesController(
            IWorkspaceService workspaceService,
            IPlatformAdminAuthorization platformAdminAuthorization)
        {
            _workspaceService = workspaceService;
            _platformAdminAuthorization = platformAdminAuthorization;
        }

        /// <summary>
        /// List all workspaces in the platform.
        /// </summary>
        /// <param name="sort">Optional sort field (`name`, `createdAt`, `updatedAt`, `id`). Prefix with `-` for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">All workspaces.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a platform administrator.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetAll)]
        [ProducesResponseType(typeof(WorkspacesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WorkspacesResponse>> GetAllWorkspaces([FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

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

            var sortedWorkspaces = ResourceSortProfiles.NamedResource.ApplyWorkspaces(
                await _workspaceService.GetWorkspacesAsync(cancellationToken),
                sortSpecification);
            var pagedWorkspaces = PaginationApplier.Apply(sortedWorkspaces, paginationSpecification);
            return Ok(pagedWorkspaces.MapToResponse());
        }

        /// <summary>
        /// Retrieve a workspace by id.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a platform administrator.</response>
        /// <response code="404">Workspace was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.Get)]
        [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceResponse>> GetWorkspace(int id, CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

            var workspace = await _workspaceService.GetWorkspaceAsync(id, cancellationToken);
            if (workspace is null)
            {
                return ApiNotFound();
            }

            return Ok(workspace.MapToResponse());
        }

        /// <summary>
        /// List members of a selected workspace.
        /// </summary>
        /// <param name="id">Workspace identifier.</param>
        /// <param name="sort">Optional sort field (`userId`, `joinedAt`, `role`). Prefix with `-` for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Members in the workspace.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a platform administrator.</response>
        /// <response code="404">Workspace not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Workspaces.GetAllMembers)]
        [ProducesResponseType(typeof(WorkspaceMembersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WorkspaceMembersResponse>> GetWorkspaceMembers(int id, [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

            var workspace = await _workspaceService.GetWorkspaceAsync(id, cancellationToken);
            if (workspace is null)
            {
                return ApiNotFound();
            }

            if (!TryParseListQuery(
                    sort,
                    page,
                    pageSize,
                    ResourceSortProfiles.WorkspaceMember.AllowedFields,
                    ResourceSortProfiles.WorkspaceMember.Default,
                    out var sortSpecification,
                    out var paginationSpecification,
                    out var problem))
            {
                return problem!;
            }

            var sortedMembers = ResourceSortProfiles.WorkspaceMember.Apply(
                await _workspaceService.GetWorkspaceMembersAsync(id, cancellationToken),
                sortSpecification);
            var pagedMembers = PaginationApplier.Apply(sortedMembers, paginationSpecification);
            return Ok(pagedMembers.MapToResponse());
        }

        private async Task<ActionResult?> RequirePlatformAdminAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _platformAdminAuthorization.EnsureCurrentUserIsPlatformAdminAsync(cancellationToken);
                return null;
            }
            catch (AccessDeniedException ex)
            {
                return ApiForbidden("forbidden", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiUnauthorized("unauthorized", ex.Message);
            }
        }
    }
}
