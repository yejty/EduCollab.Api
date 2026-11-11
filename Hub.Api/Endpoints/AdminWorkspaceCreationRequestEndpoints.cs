using Hub.Api.Mapping;
using Hub.Api.Query;
using Space.Application.Models;
using Space.Application.Services.Users;
using Space.Application.Services.Workspaces;
using Space.Contracts.Requests.Workspaces;
using Space.Contracts.Responses;
using Space.Contracts.Responses.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Api.MinimalApis;

internal static class AdminWorkspaceCreationRequestEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.AdminWorkspaceCreationRequests.GetAll, GetWorkspaceCreationRequests)
            .AsApi("AdminWorkspaceCreationRequests_GetAll", "AdminWorkspaceCreationRequests");
        routes.MapPost(ApiEndpoints.AdminWorkspaceCreationRequests.Approve, ApproveWorkspaceCreationRequest)
            .AsApi("AdminWorkspaceCreationRequests_Approve", "AdminWorkspaceCreationRequests");
        routes.MapPost(ApiEndpoints.AdminWorkspaceCreationRequests.Deny, DenyWorkspaceCreationRequest)
            .AsApi("AdminWorkspaceCreationRequests_Deny", "AdminWorkspaceCreationRequests");
    }

    /// <summary>List workspace creation requests for platform admin review.</summary>
    /// <response code="200">Paged list of creation requests.</response>
    /// <response code="400">Invalid status, sort, or pagination.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceCreationRequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> GetWorkspaceCreationRequests(
        HttpContext httpContext,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IWorkspaceCreationRequestService creationRequestService,
        IPlatformAdminAuthorization platformAdminAuthorization,
        CancellationToken cancellationToken)
    {
        var denied = await PlatformAdminGuard.RequireAsync(httpContext, platformAdminAuthorization, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        WorkspaceCreationRequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<WorkspaceCreationRequestStatus>(status, ignoreCase: true, out var value))
            {
                return ApiResults.BadRequest(httpContext, "invalid_status", "Status must be one of: Pending, Approved, Denied.");
            }

            parsedStatus = value;
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
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
            await creationRequestService.GetRequestsAsync(parsedStatus, cancellationToken),
            sortSpecification);
        return ApiResults.Ok(PaginationApplier.Apply(sortedRequests, paginationSpecification).MapToResponse());
    }

    /// <summary>Approve a pending workspace creation request and email the requester an approval token.</summary>
    /// <response code="200">Request was approved.</response>
    /// <response code="404">Request was not found.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> ApproveWorkspaceCreationRequest(
        HttpContext httpContext,
        long requestId,
        IWorkspaceCreationRequestService creationRequestService,
        IPlatformAdminAuthorization platformAdminAuthorization,
        CancellationToken cancellationToken)
    {
        var denied = await PlatformAdminGuard.RequireAsync(httpContext, platformAdminAuthorization, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        try
        {
            var approved = await creationRequestService.ApproveRequestAsync(requestId, cancellationToken);
            return approved is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(approved.MapToResponse());
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "approval_failed", ex.Message);
        }
    }

    /// <summary>Deny a pending workspace creation request and email the requester.</summary>
    /// <response code="200">Request was denied.</response>
    /// <response code="404">Request was not found.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> DenyWorkspaceCreationRequest(
        HttpContext httpContext,
        long requestId,
        DenyWorkspaceCreationRequest request,
        IWorkspaceCreationRequestService creationRequestService,
        IPlatformAdminAuthorization platformAdminAuthorization,
        CancellationToken cancellationToken)
    {
        var denied = await PlatformAdminGuard.RequireAsync(httpContext, platformAdminAuthorization, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        try
        {
            var result = await creationRequestService.DenyRequestAsync(requestId, request.Reason, cancellationToken);
            return result is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(result.MapToResponse());
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "denial_failed", ex.Message);
        }
    }
}
