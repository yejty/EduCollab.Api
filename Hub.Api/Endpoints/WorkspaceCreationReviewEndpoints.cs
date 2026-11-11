using Space.Application.Services.Notifications;
using Space.Application.Services.Workspaces;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Api.MinimalApis;

internal static class WorkspaceCreationReviewEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.WorkspaceCreationReview.Approve, ApproveFromEmail)
            .AsApi("WorkspaceCreationReview_Approve", "WorkspaceCreationReview");
        routes.MapGet(ApiEndpoints.WorkspaceCreationReview.Deny, DenyFromEmail)
            .AsApi("WorkspaceCreationReview_Deny", "WorkspaceCreationReview");
    }

    /// <summary>One-click approve action from the platform admin notification email.</summary>
    /// <response code="200">HTML success or error page.</response>
    [Produces("text/html")]
    public static async Task<IResult> ApproveFromEmail(
        long requestId,
        string reviewToken,
        IWorkspaceCreationRequestService creationRequestService,
        CancellationToken cancellationToken)
    {
        try
        {
            var approved = await creationRequestService.ApproveRequestByReviewTokenAsync(requestId, reviewToken, cancellationToken);
            if (approved is null)
            {
                return ApiResults.Text(
                    EmailActionPages.Error("Link invalid or expired", "This approve link is invalid, expired, or was already used."),
                    "text/html");
            }

            return ApiResults.Text(
                EmailActionPages.Success(
                    "Request approved",
                    $"The workspace request \"{approved.Name}\" was approved. The requester has been emailed with instructions to create their workspace."),
                "text/html");
        }
        catch (ArgumentException ex)
        {
            return ApiResults.Text(EmailActionPages.Error("Approval failed", ex.Message), "text/html");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResults.Text(EmailActionPages.Error("Approval failed", ex.Message), "text/html");
        }
    }

    /// <summary>One-click deny action from the platform admin notification email.</summary>
    /// <response code="200">HTML success or error page.</response>
    [Produces("text/html")]
    public static async Task<IResult> DenyFromEmail(
        long requestId,
        string reviewToken,
        IWorkspaceCreationRequestService creationRequestService,
        CancellationToken cancellationToken)
    {
        try
        {
            var denied = await creationRequestService.DenyRequestByReviewTokenAsync(requestId, reviewToken, cancellationToken);
            if (denied is null)
            {
                return ApiResults.Text(
                    EmailActionPages.Error("Link invalid or expired", "This deny link is invalid, expired, or was already used."),
                    "text/html");
            }

            return ApiResults.Text(
                EmailActionPages.Success(
                    "Request denied",
                    $"The workspace request \"{denied.Name}\" was denied. The requester has been emailed with the outcome."),
                "text/html");
        }
        catch (ArgumentException ex)
        {
            return ApiResults.Text(EmailActionPages.Error("Denial failed", ex.Message), "text/html");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResults.Text(EmailActionPages.Error("Denial failed", ex.Message), "text/html");
        }
    }
}
