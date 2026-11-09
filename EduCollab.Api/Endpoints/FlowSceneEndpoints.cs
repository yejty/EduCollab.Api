using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Responses.Flows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.MinimalApis;

internal static class FlowSceneEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.FlowScenes.GetAll, GetFlowScenes).AsApi("FlowScenes_GetAll", "FlowScenes");
        routes.MapGet(ApiEndpoints.FlowScenes.Content, GetFlowSceneContent).AsApi("FlowScenes_GetContent", "FlowScenes");
    }

    /// <summary>List scenes attached to a flow.</summary>
    /// <response code="200">Flow scene manifest.</response>
    [Authorize]
    [ProducesResponseType(typeof(FlowScenesResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetFlowScenes(
        HttpContext httpContext,
        [FromQuery] int flowId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (flowId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_flow_id", "flowId is required and must be a positive integer.");
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.FlowScene.AllowedFields,
                ResourceSortProfiles.FlowScene.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        try
        {
            var manifest = await flowService.GetFlowScenesAsync(flowId, cancellationToken);
            var sorted = ResourceSortProfiles.FlowScene.Apply(manifest.Scenes, sortSpecification);
            return ApiResults.Ok(PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse(manifest));
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Download scene JSON using a short-lived flow-scenes download token.</summary>
    /// <response code="200">Scene JSON file.</response>
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetFlowSceneContent(
        HttpContext httpContext,
        [FromQuery] string? token,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ApiResults.BadRequest(httpContext, "invalid_download_token", "token is required.");
        }

        try
        {
            var content = await flowService.GetFlowSceneContentAsync(token, cancellationToken);
            if (content is null)
            {
                return ApiResults.NotFound(httpContext);
            }

            var contentType = string.IsNullOrWhiteSpace(content.ContentType) ? "application/json" : content.ContentType;
            var fileName = string.IsNullOrWhiteSpace(content.FileName) ? "scene.json" : content.FileName;
            return ApiResults.File(content.Data, contentType, fileName);
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}
