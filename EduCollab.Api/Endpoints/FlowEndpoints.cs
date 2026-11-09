using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Api.Swagger;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Responses.Flows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.MinimalApis;

internal static class FlowEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.Flows.Create, CreateFlow).AsApi("Flows_Create", "Flows");
        routes.MapGet(ApiEndpoints.Flows.GetAll, GetFlows).AsApi("Flows_GetAll", "Flows");
        routes.MapGet(ApiEndpoints.Flows.Get, GetFlow).AsApi("Flows_Get", "Flows");
        routes.MapPut(ApiEndpoints.Flows.Update, UpdateFlow).AsApi("Flows_Update", "Flows");
        routes.MapDelete(ApiEndpoints.Flows.Delete, DeleteFlow).AsApi("Flows_Delete", "Flows");
    }

    /// <summary>Create a new flow.</summary>
    /// <response code="201">Flow was created.</response>
    /// <response code="400">Flow could not be created.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows")]
    [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> CreateFlow(
        HttpContext httpContext,
        CreateFlowRequest request,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        var flow = request.MapToFlow();
        var created = await flowService.CreateFlowAsync(
            flow,
            ResourceGroupPlacement.ResolveGroupIds(request.GroupIds),
            request.SceneIds,
            cancellationToken);
        if (!created)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "Flow could not be created.");
        }

        var response = flow.MapToResponse();
        response.CanManage = await flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);
        return ApiResults.Created(ApiRoutes.For(ApiEndpoints.Flows.Get, new { flowId = flow.Id }), response);
    }

    /// <summary>List accessible flows in the current workspace.</summary>
    /// <response code="200">Paged list of flows.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadFlows", "addFlows", Notes = "Also requires effective access to each flow (ownership or group membership).")]
    [ProducesResponseType(typeof(FlowsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetFlows(
        HttpContext httpContext,
        [FromQuery] string? owner,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (!OwnerQueryParser.TryParse(owner, out var ownerIsCurrentUser, out var ownerError))
        {
            return ApiResults.BadRequest(httpContext, "invalid_filter", ownerError!);
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

        var flows = ownerIsCurrentUser
            ? await flowService.GetMyFlowsAsync(cancellationToken)
            : await flowService.GetAllFlowsAsync(cancellationToken);
        var sorted = ResourceSortProfiles.NamedResource.ApplyFlows(flows, sortSpecification);
        var response = PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse();
        foreach (var flow in response.Flows)
        {
            flow.CanManage = await flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);
        }

        return ApiResults.Ok(response);
    }

    /// <summary>Retrieve a flow by id.</summary>
    /// <response code="200">Returns the flow.</response>
    /// <response code="404">Flow was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadFlows", "addFlows", Notes = "Also requires effective access to the flow (ownership or group membership).")]
    [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetFlow(
        HttpContext httpContext,
        int flowId,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        var flow = await flowService.GetFlowByIdAsync(flowId, cancellationToken);
        if (flow is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var response = flow.MapToResponse();
        response.CanManage = await flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);
        return ApiResults.Ok(response);
    }

    /// <summary>Update flow metadata and attached scenes.</summary>
    /// <response code="200">Flow was updated.</response>
    /// <response code="404">Flow was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the flow.")]
    [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> UpdateFlow(
        HttpContext httpContext,
        int flowId,
        UpdateFlowRequest request,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        var updated = await flowService.UpdateFlowAsync(request.MapToFlow(flowId), request.SceneIds, cancellationToken);
        if (updated is null)
        {
            return ApiResults.NotFound(httpContext, "update_failed", "Flow was not found.");
        }

        var response = updated.MapToResponse();
        response.CanManage = await flowService.CanCurrentUserManageFlowAsync(updated.OwnerUserId, cancellationToken);
        return ApiResults.Ok(response);
    }

    /// <summary>Delete a flow.</summary>
    /// <response code="204">Flow was deleted.</response>
    /// <response code="404">Flow was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the flow.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> DeleteFlow(
        HttpContext httpContext,
        int flowId,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        var deleted = await flowService.DeleteFlowAsync(flowId, cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }
}
