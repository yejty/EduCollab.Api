using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Scenes;
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
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Scenes.Create)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateScene([FromBody] CreateSceneRequest request, CancellationToken cancellationToken)
        {
            var scene = request.MapToScene();
            var created = await _sceneService.CreateSceneAsync(scene, request.GroupId, cancellationToken);
            if (!created)
                return ApiBadRequest("creation_failed", "Scene could not be created.");

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return CreatedAtAction(nameof(GetScene), new { sceneId = scene.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.GetAll)]
        [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]
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

            var sorted = ResourceSortProfiles.NamedResource.ApplyScenes(scenes, sortSpecification);
            var paged = PaginationApplier.Apply(sorted, paginationSpecification);
            var response = paged.MapToResponse();

            foreach (var scene in response.Scenes)
                await PopulateAccessMetadataAsync(scene, cancellationToken);

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.Get)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SceneResponse>> GetScene(int sceneId, CancellationToken cancellationToken)
        {
            var scene = await _sceneService.GetSceneByIdAsync(sceneId, cancellationToken);
            if (scene is null)
                return ApiNotFound();

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Scenes.Update)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SceneResponse>> UpdateScene(int sceneId, [FromBody] UpdateSceneRequest request, CancellationToken cancellationToken)
        {
            var scene = request.MapToScene(sceneId);
            var updated = await _sceneService.UpdateSceneAsync(scene, cancellationToken);
            if (updated is null)
                return ApiNotFound("update_failed", "Scene was not found.");

            var response = updated.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Scenes.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteScene(int sceneId, CancellationToken cancellationToken)
        {
            var deleted = await _sceneService.DeleteSceneAsync(sceneId, cancellationToken);
            if (!deleted)
                return ApiNotFound();

            return NoContent();
        }
    }
}
