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
    }
}
