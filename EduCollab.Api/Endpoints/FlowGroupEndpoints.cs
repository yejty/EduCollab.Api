using EduCollab.Api.Swagger;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.MinimalApis;

internal static class FlowGroupEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.FlowGroups.GetAll, GetFlowGroups).AsApi("FlowGroups_GetAll", "FlowGroups");
        routes.MapPost(ApiEndpoints.FlowGroups.Create, AddFlowGroup).AsApi("FlowGroups_Create", "FlowGroups");
        routes.MapPut(ApiEndpoints.FlowGroups.Update, SetFlowGroups).AsApi("FlowGroups_Update", "FlowGroups");
        routes.MapDelete(ApiEndpoints.FlowGroups.Delete, RemoveFlowGroup).AsApi("FlowGroups_Delete", "FlowGroups");
    }

    /// <summary>List groups a flow is shared with.</summary>
    /// <response code="200">Groups returned.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows", Notes = "Also requires effective access to the flow.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetFlowGroups(
        HttpContext httpContext,
        [FromQuery] int flowId,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (flowId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_flow_id", "flowId is required and must be a positive integer.");
        }

        try
        {
            var shares = await flowService.GetFlowGroupSharesAsync(flowId, cancellationToken);
            return ApiResults.Ok(MapShares(shares));
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Share a flow with a group.</summary>
    /// <response code="201">Flow was shared.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows", Notes = "Also requires manage access to the flow.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> AddFlowGroup(
        HttpContext httpContext,
        AttachFlowGroupRequest request,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (request.FlowId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_flow_id", "flowId must be a positive integer.");
        }

        if (request.GroupId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId must be a positive integer.");
        }

        var added = await flowService.AddFlowGroupAsync(request.FlowId, request.GroupId, request.IncludeAssets, cancellationToken);
        if (!added)
        {
            return ApiResults.NotFound(httpContext, "share_failed", "Flow or group was not found.");
        }

        var shares = await flowService.GetFlowGroupSharesAsync(request.FlowId, cancellationToken);
        return ApiResults.Status(StatusCodes.Status201Created, MapShares(shares));
    }

    /// <summary>Replace all group shares for a flow.</summary>
    /// <response code="200">Group shares were replaced.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows", Notes = "Also requires manage access to the flow.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> SetFlowGroups(
        HttpContext httpContext,
        [FromQuery] int flowId,
        SetFlowGroupsRequest request,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (flowId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_flow_id", "flowId is required and must be a positive integer.");
        }

        try
        {
            var shares = (request.Groups ?? [])
                .Select(group => new FlowGroupShare
                {
                    GroupId = group.GroupId,
                    IncludeAssets = group.IncludeAssets,
                })
                .ToList();
            var updated = await flowService.SetFlowGroupSharesAsync(flowId, shares, cancellationToken);
            return updated is null
                ? ApiResults.NotFound(httpContext, "update_failed", "Flow was not found.")
                : ApiResults.Ok(MapShares(updated));
        }
        catch (ArgumentException ex) when (ex.ParamName == "groupIds")
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", ex.Message);
        }
    }

    /// <summary>Remove a flow from a group.</summary>
    /// <response code="204">Flow was removed from the group.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addFlows", Notes = "Also requires manage access to the flow.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> RemoveFlowGroup(
        HttpContext httpContext,
        [FromQuery] int flowId,
        [FromQuery] int groupId,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (flowId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_flow_id", "flowId is required and must be a positive integer.");
        }

        if (groupId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId is required and must be a positive integer.");
        }

        var removed = await flowService.RemoveFlowGroupAsync(flowId, groupId, cancellationToken);
        return removed ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    private static ResourceGroupsResponse MapShares(IReadOnlyList<FlowGroupShare> shares) =>
        new()
        {
            GroupIds = shares.Select(share => share.GroupId).ToList(),
            Groups = shares
                .Select(share => new ResourceGroupShareResponse
                {
                    GroupId = share.GroupId,
                    IncludeAssets = share.IncludeAssets,
                })
                .ToList(),
        };
}
