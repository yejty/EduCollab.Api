using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Scenes;
using EduCollab.Contracts.Requests.Scenes;
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
        /// List assets attached to a scene.
        /// </summary>
        /// <remarks>
        /// Authoritative manifest for rendering a scene. Merges assets referenced by <c>assetId</c> in scene JSON
        /// with explicit scene attachments. Use <see cref="GetSceneAssetContent"/> to download ZIP content when
        /// <c>canViewDirectly</c> is false.
        /// </remarks>
        /// <param name="sceneId">Scene identifier (required).</param>
        /// <param name="sort">Optional sort field (<c>name</c>, <c>assetId</c>). Prefix with <c>-</c> for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Paged list of scene assets.</response>
        /// <response code="400">Invalid scene id, sort, or pagination.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this scene.</response>
        /// <response code="404">Scene was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.SceneAssets.GetAll)]
        [ProducesResponseType(typeof(SceneAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneAssetsResponse>> GetSceneAssets(
            [FromQuery] int sceneId,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

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
                var assets = await _sceneService.GetSceneAssetsAsync(sceneId, cancellationToken);
                var sorted = ResourceSortProfiles.SceneAsset.Apply(assets, sortSpecification);
                var paged = PaginationApplier.Apply(sorted, paginationSpecification);
                return Ok(paged.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Attach an asset to a scene.
        /// </summary>
        /// <param name="request">Scene and asset identifiers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Asset was attached to the scene.</response>
        /// <response code="400">Invalid scene or asset id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot modify this scene.</response>
        /// <response code="404">Scene or asset was not found.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.SceneAssets.Create)]
        [ProducesResponseType(typeof(SceneAssetResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneAssetResponse>> AttachSceneAsset(
            [FromBody] AttachSceneAssetRequest request,
            CancellationToken cancellationToken)
        {
            if (request.SceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId must be a positive integer.");

            if (request.AssetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId must be a positive integer.");

            var attached = await _sceneService.AttachSceneAssetAsync(request.SceneId, request.AssetId, cancellationToken);
            if (attached is null)
                return ApiNotFound("attach_failed", "Scene or asset was not found.");

            return StatusCode(StatusCodes.Status201Created, attached.MapToResponse());
        }

        /// <summary>
        /// Detach an asset from a scene.
        /// </summary>
        /// <param name="sceneId">Scene identifier (required).</param>
        /// <param name="assetId">Asset identifier (required).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Asset was detached from the scene.</response>
        /// <response code="400">Invalid scene or asset id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot modify this scene.</response>
        /// <response code="404">Scene-asset link was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.SceneAssets.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DetachSceneAsset(
            [FromQuery] int sceneId,
            [FromQuery] int assetId,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            var detached = await _sceneService.DetachSceneAssetAsync(sceneId, assetId, cancellationToken);
            if (!detached)
                return ApiNotFound();

            return NoContent();
        }

        /// <summary>
        /// Download asset ZIP content in scene context.
        /// </summary>
        /// <remarks>
        /// Use when the manifest lists an asset with <c>usableInScene: true</c> and <c>canViewDirectly: false</c>.
        /// Standalone <c>GET /assets/{assetId}/content</c> remains direct-access only.
        /// </remarks>
        /// <param name="sceneId">Scene identifier (required).</param>
        /// <param name="assetId">Asset identifier (required).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Asset ZIP content.</response>
        /// <response code="400">Invalid scene or asset id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this scene.</response>
        /// <response code="404">Scene, asset, or content was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.SceneAssets.Content)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSceneAssetContent(
            [FromQuery] int sceneId,
            [FromQuery] int assetId,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            try
            {
                var content = await _sceneService.GetSceneAssetContentAsync(sceneId, assetId, cancellationToken);
                if (content is null)
                    return ApiNotFound();

                return File(content.Data, content.ContentType);
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
