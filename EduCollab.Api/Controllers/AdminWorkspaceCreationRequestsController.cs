using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AdminWorkspaceCreationRequestsController : ApiControllerBase
    {
        private readonly IWorkspaceCreationRequestService _creationRequestService;
        private readonly IPlatformAdminAuthorization _platformAdminAuthorization;

        public AdminWorkspaceCreationRequestsController(
            IWorkspaceCreationRequestService creationRequestService,
            IPlatformAdminAuthorization platformAdminAuthorization)
        {
            _creationRequestService = creationRequestService;
            _platformAdminAuthorization = platformAdminAuthorization;
        }

        /// <summary>
        /// List workspace creation requests for platform admin review.
        /// </summary>
        /// <param name="status">Optional status filter: <c>Pending</c>, <c>Approved</c>, or <c>Denied</c>.</param>
        /// <param name="sort">Optional sort field (<c>name</c>, <c>createdAt</c>, <c>status</c>, <c>id</c>). Prefix with <c>-</c> for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Paged list of creation requests.</response>
        /// <response code="400">Invalid status, sort, or pagination.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a platform administrator.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.AdminWorkspaceCreationRequests.GetAll)]
        [ProducesResponseType(typeof(WorkspaceCreationRequestsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WorkspaceCreationRequestsResponse>> GetWorkspaceCreationRequests(
            [FromQuery] string? status,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

            WorkspaceCreationRequestStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<WorkspaceCreationRequestStatus>(status, ignoreCase: true, out var value))
                {
                    return ApiBadRequest(
                        "invalid_status",
                        "Status must be one of: Pending, Approved, Denied.");
                }

                parsedStatus = value;
            }

            if (!TryParseListQuery(
                    sort,
                    page,
                    pageSize,
                    ResourceSortProfiles.WorkspaceCreationRequest.AllowedFields,
                    ResourceSortProfiles.WorkspaceCreationRequest.Default,
                    out var sortSpecification,
                    out var paginationSpecification,
                    out var problem))
            {
                return problem!;
            }

            var sortedRequests = ResourceSortProfiles.WorkspaceCreationRequest.Apply(
                await _creationRequestService.GetRequestsAsync(parsedStatus, cancellationToken),
                sortSpecification);
            var pagedRequests = PaginationApplier.Apply(sortedRequests, paginationSpecification);
            return Ok(pagedRequests.MapToResponse());
        }

        /// <summary>
        /// Approve a pending workspace creation request and email the requester an approval token.
        /// </summary>
        /// <param name="requestId">Creation request identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Request was approved.</response>
        /// <response code="400">Request could not be approved.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a platform administrator.</response>
        /// <response code="404">Request was not found.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.AdminWorkspaceCreationRequests.Approve)]
        [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceCreationRequestResponse>> ApproveWorkspaceCreationRequest(
            long requestId,
            CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

            try
            {
                var approved = await _creationRequestService.ApproveRequestAsync(requestId, cancellationToken);
                if (approved is null)
                    return ApiNotFound();

                return Ok(approved.MapToResponse());
            }
            catch (ArgumentException ex)
            {
                return ApiBadRequest("approval_failed", ex.Message);
            }
        }

        /// <summary>
        /// Deny a pending workspace creation request and email the requester.
        /// </summary>
        /// <param name="requestId">Creation request identifier.</param>
        /// <param name="request">Optional denial reason.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Request was denied.</response>
        /// <response code="400">Request could not be denied.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller is not a platform administrator.</response>
        /// <response code="404">Request was not found.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.AdminWorkspaceCreationRequests.Deny)]
        [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkspaceCreationRequestResponse>> DenyWorkspaceCreationRequest(
            long requestId,
            [FromBody] DenyWorkspaceCreationRequest request,
            CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

            try
            {
                var result = await _creationRequestService.DenyRequestAsync(requestId, request.Reason, cancellationToken);
                if (result is null)
                    return ApiNotFound();

                return Ok(result.MapToResponse());
            }
            catch (ArgumentException ex)
            {
                return ApiBadRequest("denial_failed", ex.Message);
            }
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
