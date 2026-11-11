using Hub.Api.Mapping;
using Hub.Api.Query;
using Space.Application.Services.Users;
using Space.Application.Services.Workspaces;
using Space.Contracts.Responses;
using Space.Contracts.Responses.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Api.MinimalApis;

internal static class AdminWorkspaceEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.Workspaces.GetAll, GetAllWorkspaces).AsApi("AdminWorkspaces_GetAll", "AdminWorkspaces");
        routes.MapGet(ApiEndpoints.Workspaces.Get, GetWorkspace).AsApi("AdminWorkspaces_Get", "AdminWorkspaces");
        routes.MapGet(ApiEndpoints.Workspaces.GetAllMembers, GetWorkspaceMembers).AsApi("AdminWorkspaces_GetMembers", "AdminWorkspaces");
    }

    /// <summary>List all workspaces in the platform.</summary>
    /// <response code="200">All workspaces.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is not a platform administrator.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspacesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> GetAllWorkspaces(
        HttpContext httpContext,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IWorkspaceService workspaceService,
        IPlatformAdminAuthorization platformAdminAuthorization,
        CancellationToken cancellationToken)
    {
        var denied = await PlatformAdminGuard.RequireAsync(httpContext, platformAdminAuthorization, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
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
            await workspaceService.GetWorkspacesAsync(cancellationToken),
            sortSpecification);
        return ApiResults.Ok(PaginationApplier.Apply(sortedWorkspaces, paginationSpecification).MapToResponse());
    }

    /// <summary>Retrieve a workspace by id.</summary>
    /// <response code="200">Returns the workspace.</response>
    /// <response code="404">Workspace was not found.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetWorkspace(
        HttpContext httpContext,
        int id,
        IWorkspaceService workspaceService,
        IPlatformAdminAuthorization platformAdminAuthorization,
        CancellationToken cancellationToken)
    {
        var denied = await PlatformAdminGuard.RequireAsync(httpContext, platformAdminAuthorization, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        var workspace = await workspaceService.GetWorkspaceAsync(id, cancellationToken);
        return workspace is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(workspace.MapToResponse());
    }

    /// <summary>List members of a selected workspace.</summary>
    /// <response code="200">Members in the workspace.</response>
    /// <response code="404">Workspace not found.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceMembersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public static async Task<IResult> GetWorkspaceMembers(
        HttpContext httpContext,
        int id,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IWorkspaceService workspaceService,
        IPlatformAdminAuthorization platformAdminAuthorization,
        CancellationToken cancellationToken)
    {
        var denied = await PlatformAdminGuard.RequireAsync(httpContext, platformAdminAuthorization, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        var workspace = await workspaceService.GetWorkspaceAsync(id, cancellationToken);
        if (workspace is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
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
            await workspaceService.GetWorkspaceMembersAsync(id, cancellationToken),
            sortSpecification);
        return ApiResults.Ok(PaginationApplier.Apply(sortedMembers, paginationSpecification).MapToResponse());
    }
}
