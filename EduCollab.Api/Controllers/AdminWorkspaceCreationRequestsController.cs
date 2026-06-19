using EduCollab.Api.Mapping;
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
    public class AdminWorkspaceCreationRequestsController : ControllerBase
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
        [Authorize]
        [HttpGet(ApiEndpoints.AdminWorkspaceCreationRequests.GetAll)]
        [ProducesResponseType(typeof(WorkspaceCreationRequestsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<WorkspaceCreationRequestsResponse>> GetWorkspaceCreationRequests(
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            var denied = await RequirePlatformAdminAsync(cancellationToken);
            if (denied is not null)
                return denied;

            WorkspaceCreationRequestStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<WorkspaceCreationRequestStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
            }

            var requests = await _creationRequestService.GetRequestsAsync(parsedStatus, cancellationToken);
            return Ok(requests.MapToResponse());
        }

        /// <summary>
        /// Approve a pending workspace creation request and email the requester an approval token.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.AdminWorkspaceCreationRequests.Approve)]
        [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
                    return NotFound();

                return Ok(approved.MapToResponse());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "approval_failed",
                    ErrorDescription = ex.Message,
                });
            }
        }

        /// <summary>
        /// Deny a pending workspace creation request and email the requester.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.AdminWorkspaceCreationRequests.Deny)]
        [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
                    return NotFound();

                return Ok(result.MapToResponse());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "denial_failed",
                    ErrorDescription = ex.Message,
                });
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
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
                {
                    Error = "forbidden",
                    ErrorDescription = ex.Message,
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ErrorResponse
                {
                    Error = "unauthorized",
                    ErrorDescription = ex.Message,
                });
            }
        }
    }
}
