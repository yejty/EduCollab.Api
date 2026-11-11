using Drive.Api.Mapping;
using Drive.Api.Query;
using Space.Application.Services.Scenes;
using Space.Contracts.Responses.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.MinimalApis;

internal static class SceneAssetEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.SceneAssets.GetAll, GetSceneAssets).AsApi("SceneAssets_GetAll", "SceneAssets");
        routes.MapGet(ApiEndpoints.SceneAssets.Content, GetSceneAssetContent).AsApi("SceneAssets_GetContent", "SceneAssets");
    }

    /// <summary>List assets referenced by a scene.</summary>
    /// <response code="200">Scene asset manifest.</response>
    [Authorize]
    [ProducesResponseType(typeof(SceneAssetsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetSceneAssets(
        HttpContext httpContext,
        [FromQuery] int sceneId,
        [FromQuery] int? flowId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        if (flowId is <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_flow_id", "flowId must be a positive integer when provided.");
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.SceneAsset.AllowedFields,
                ResourceSortProfiles.SceneAsset.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        try
        {
            var manifest = await sceneService.GetSceneAssetsAsync(sceneId, flowId, cancellationToken);
            var sorted = ResourceSortProfiles.SceneAsset.Apply(manifest.Assets, sortSpecification);
            return ApiResults.Ok(PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse(manifest));
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Download asset ZIP content using a short-lived scene-assets download token.</summary>
    /// <response code="200">Asset ZIP bytes.</response>
    [Authorize]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetSceneAssetContent(
        HttpContext httpContext,
        [FromQuery] string? token,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ApiResults.BadRequest(httpContext, "invalid_download_token", "token is required.");
        }

        try
        {
            var content = await sceneService.GetSceneAssetContentAsync(token, cancellationToken);
            if (content is null)
            {
                return ApiResults.NotFound(httpContext);
            }

            var contentType = string.IsNullOrWhiteSpace(content.ContentType) ? "application/zip" : content.ContentType;
            return ApiResults.File(content.Data, contentType, "asset.zip");
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}
