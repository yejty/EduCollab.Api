using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Services.Scenes;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Requests.Scenes;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class ScenesController : ApiControllerBase
    {
        private readonly ISceneService _sceneService;

        public ScenesController(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        private async Task PopulateAccessMetadataAsync(SceneResponse scene, CancellationToken cancellationToken)
        {
            scene.CanEdit = await _sceneService.CanCurrentUserManageSceneAsync(scene.OwnerUserId, cancellationToken);
            scene.CanManage = scene.CanEdit;
            scene.GroupIds = await _sceneService.GetSceneGroupIdsAsync(scene.Id, cancellationToken);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Scenes.Create)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateScene([FromBody] CreateSceneRequest request, CancellationToken cancellationToken)
        {
            var scene = request.MapToScene();
            var created = await _sceneService.CreateSceneAsync(scene, request.GroupId, cancellationToken);
            if (!created)
            {
                return ApiBadRequest("creation_failed", "Scene could not be created.");
            }

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return CreatedAtAction(nameof(GetScene), new { sceneId = scene.Id }, response);
        }

        private async Task<ActionResult<ScenesResponse>> BuildScenesResponseAsync(
            List<Application.Models.Scene> scenes,
            SortSpecification sortSpecification,
            PaginationSpecification paginationSpecification,
            CancellationToken cancellationToken)
        {
            var sorted = ResourceSortProfiles.NamedResource.ApplyScenes(scenes, sortSpecification);
            var paged = PaginationApplier.Apply(sorted, paginationSpecification);
            var response = paged.MapToResponse();

            foreach (var scene in response.Scenes)
            {
                await PopulateAccessMetadataAsync(scene, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.GetAll)]
        [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ScenesResponse>> GetScenes(
            [FromQuery] string? owner,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (!OwnerQueryParser.TryParse(owner, out var ownerIsCurrentUser, out var ownerError))
                return ApiBadRequest("invalid_filter", ownerError!);

            if (!TryParseListQuery(
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

            var scenes = ownerIsCurrentUser
                ? await _sceneService.GetMyScenesAsync(cancellationToken)
                : await _sceneService.GetAllScenesAsync(cancellationToken);
            return await BuildScenesResponseAsync(scenes, sortSpecification, paginationSpecification, cancellationToken);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.Get)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneResponse>> GetScene(int sceneId, [FromQuery] int? versionNumber, CancellationToken cancellationToken)
        {
            var scene = await _sceneService.GetSceneByIdAsync(sceneId, versionNumber, cancellationToken);
            if (scene is null)
            {
                return ApiNotFound();
            }

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return Ok(response);
        }

        /// <summary>
        /// Replace a scene document. Requires the current scene ETag in the If-Match request header.
        /// </summary>
        /// <param name="sceneId">Scene identifier.</param>
        /// <param name="request">Full scene payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Scene was updated.</response>
        /// <response code="400">If-Match header is missing.</response>
        /// <response code="412">If-Match does not match the current scene version.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Scenes.Update)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
        public async Task<ActionResult<SceneResponse>> UpdateScene(int sceneId, [FromBody] UpdateSceneRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetRequiredIfMatchHeader(out var ifMatch, out var missingIfMatch))
            {
                return missingIfMatch!;
            }

            var scene = request.MapToScene(sceneId);
            try
            {
                var updated = await _sceneService.UpdateSceneAsync(scene, ifMatch, cancellationToken);
                if (updated is null)
                {
                    return ApiNotFound("update_failed", "Scene was not found.");
                }

                var response = updated.MapToResponse();
                await PopulateAccessMetadataAsync(response, cancellationToken);
                Response.Headers.ETag = updated.ETag;
                return Ok(response);
            }
            catch (PreconditionFailedException ex)
            {
                return ApiPreconditionFailed("precondition_failed", ex.Message);
            }
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Scenes.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteScene(int sceneId, CancellationToken cancellationToken)
        {
            var deleted = await _sceneService.DeleteSceneAsync(sceneId, cancellationToken);
            if (!deleted)
            {
                return ApiNotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.GetVersions)]
        [ProducesResponseType(typeof(SceneVersionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneVersionsResponse>> GetSceneVersions(int sceneId, CancellationToken cancellationToken)
        {
            var scene = await _sceneService.GetSceneByIdAsync(sceneId, null, cancellationToken);
            if (scene is null)
                return ApiNotFound();

            var versions = await _sceneService.GetSceneVersionsAsync(sceneId, cancellationToken);
            return Ok(versions.MapToResponse());
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.GetVersion)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneResponse>> GetSceneVersion(int sceneId, int versionNumber, CancellationToken cancellationToken)
        {
            var scene = await _sceneService.GetSceneByIdAsync(sceneId, versionNumber, cancellationToken);
            if (scene is null)
                return ApiNotFound();

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return Ok(response);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Scenes.Share)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneResponse>> ShareScene(int sceneId, [FromBody] ShareWithGroupRequest request, CancellationToken cancellationToken)
        {
            var shared = await _sceneService.ShareSceneAsync(sceneId, request.GroupId, cancellationToken);
            if (!shared)
            {
                return ApiBadRequest("sharing_failed", "Scene could not be shared with the group.");
            }

            var scene = await _sceneService.GetSceneByIdAsync(sceneId, null, cancellationToken);
            if (scene is null)
                return ApiNotFound();

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Scenes.Unshare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveSceneShare(int sceneId, int groupId, CancellationToken cancellationToken)
        {
            var removed = await _sceneService.RemoveSceneShareAsync(sceneId, groupId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
