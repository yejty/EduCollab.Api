using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Scenes;
using EduCollab.Contracts.Responses.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class SceneAssetsController : ApiControllerBase
    {
        private readonly ISceneService _sceneService;

        public SceneAssetsController(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        /// <summary>
        /// List assets referenced by a scene (JSON assetId values).
        /// </summary>
        /// <remarks>
        /// Peer-visible assets have <c>canViewDirectly=true</c> and should be downloaded via
        /// <c>GET /assets/{assetId}/content</c>. Contextual assets include a short-lived
        /// <c>downloadToken</c> when the scene (or flow, when <c>flowId</c> is set) share has
        /// <c>includeAssets=true</c>. Pass <c>flowId</c> when loading assets nested under a flow.
        /// </remarks>
        [Authorize]
        [HttpGet(ApiEndpoints.SceneAssets.GetAll)]
        [ProducesResponseType(typeof(SceneAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneAssetsResponse>> GetSceneAssets(
            [FromQuery] int sceneId,
            [FromQuery] int? flowId,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            if (flowId is <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId must be a positive integer when provided.");

            if (!TryParseListQuery(
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
                var manifest = await _sceneService.GetSceneAssetsAsync(sceneId, flowId, cancellationToken);
                var sorted = ResourceSortProfiles.SceneAsset.Apply(manifest.Assets, sortSpecification);
                var paged = PaginationApplier.Apply(sorted, paginationSpecification);
                return Ok(paged.MapToResponse(manifest));
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Download asset ZIP content using a short-lived scene-assets download token.
        /// </summary>
        /// <remarks>
        /// Requires the caller access token and a <c>downloadToken</c> from the scene-assets manifest.
        /// Peer-visible assets should use <c>GET /assets/{assetId}/content</c> instead.
        /// </remarks>
        [Authorize]
        [HttpGet(ApiEndpoints.SceneAssets.Content)]
        [Produces("application/zip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSceneAssetContent(
            [FromQuery] string? token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiBadRequest("invalid_download_token", "token is required.");

            try
            {
                var content = await _sceneService.GetSceneAssetContentAsync(token, cancellationToken);
                if (content is null)
                    return ApiNotFound();

                var contentType = string.IsNullOrWhiteSpace(content.ContentType)
                    ? "application/zip"
                    : content.ContentType;

                return File(content.Data, contentType, fileDownloadName: "asset.zip");
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
