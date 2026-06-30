using EduCollab.Application.Services.Notifications;
using EduCollab.Application.Services.Workspaces;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class WorkspaceCreationReviewController : ApiControllerBase
    {
        private readonly IWorkspaceCreationRequestService _creationRequestService;

        public WorkspaceCreationReviewController(IWorkspaceCreationRequestService creationRequestService)
        {
            _creationRequestService = creationRequestService;
        }

        /// <summary>
        /// One-click approve action from the platform admin notification email.
        /// </summary>
        /// <param name="requestId">Creation request identifier.</param>
        /// <param name="reviewToken">Signed review token from the email link.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">HTML success or error page.</response>
        [HttpGet(ApiEndpoints.WorkspaceCreationReview.Approve)]
        [Produces("text/html")]
        public async Task<IActionResult> ApproveFromEmail(
            [FromRoute] long requestId,
            [FromRoute] string reviewToken,
            CancellationToken cancellationToken)
        {
            try
            {
                var approved = await _creationRequestService.ApproveRequestByReviewTokenAsync(requestId, reviewToken, cancellationToken);
                if (approved is null)
                {
                    return Content(
                        EmailActionPages.Error(
                            "Link invalid or expired",
                            "This approve link is invalid, expired, or was already used."),
                        "text/html");
                }

                return Content(
                    EmailActionPages.Success(
                        "Request approved",
                        $"The workspace request \"{approved.Name}\" was approved. The requester has been emailed with instructions to create their workspace."),
                    "text/html");
            }
            catch (ArgumentException ex)
            {
                return Content(EmailActionPages.Error("Approval failed", ex.Message), "text/html");
            }
            catch (InvalidOperationException ex)
            {
                return Content(EmailActionPages.Error("Approval failed", ex.Message), "text/html");
            }
        }

        /// <summary>
        /// One-click deny action from the platform admin notification email.
        /// </summary>
        /// <param name="requestId">Creation request identifier.</param>
        /// <param name="reviewToken">Signed review token from the email link.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">HTML success or error page.</response>
        [HttpGet(ApiEndpoints.WorkspaceCreationReview.Deny)]
        [Produces("text/html")]
        public async Task<IActionResult> DenyFromEmail(
            [FromRoute] long requestId,
            [FromRoute] string reviewToken,
            CancellationToken cancellationToken)
        {
            try
            {
                var denied = await _creationRequestService.DenyRequestByReviewTokenAsync(requestId, reviewToken, cancellationToken);
                if (denied is null)
                {
                    return Content(
                        EmailActionPages.Error(
                            "Link invalid or expired",
                            "This deny link is invalid, expired, or was already used."),
                        "text/html");
                }

                return Content(
                    EmailActionPages.Success(
                        "Request denied",
                        $"The workspace request \"{denied.Name}\" was denied. The requester has been emailed with the outcome."),
                    "text/html");
            }
            catch (ArgumentException ex)
            {
                return Content(EmailActionPages.Error("Denial failed", ex.Message), "text/html");
            }
            catch (InvalidOperationException ex)
            {
                return Content(EmailActionPages.Error("Denial failed", ex.Message), "text/html");
            }
        }
    }
}
