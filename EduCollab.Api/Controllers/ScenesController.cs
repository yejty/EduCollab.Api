using EduCollab.Api.Mapping;
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
    public class ScenesController : ControllerBase
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
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateScene([FromBody] CreateSceneRequest request, CancellationToken cancellationToken)
        {
            var scene = request.MapToScene();
            var created = await _sceneService.CreateSceneAsync(scene, request.GroupId, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Scene could not be created."
                });
            }

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return CreatedAtAction(nameof(GetScene), new { sceneId = scene.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.GetAll)]
        [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ScenesResponse>> GetScenes(CancellationToken cancellationToken)
        {
            var scenes = await _sceneService.GetAllScenesAsync(cancellationToken);
            var response = scenes.MapToResponse();

            foreach (var scene in response.Scenes)
            {
                await PopulateAccessMetadataAsync(scene, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.GetMine)]
        [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ScenesResponse>> GetMyScenes(CancellationToken cancellationToken)
        {
            var scenes = await _sceneService.GetMyScenesAsync(cancellationToken);
            var response = scenes.MapToResponse();

            foreach (var scene in response.Scenes)
            {
                await PopulateAccessMetadataAsync(scene, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Scenes.Get)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneResponse>> GetScene(int sceneId, CancellationToken cancellationToken)
        {
            var scene = await _sceneService.GetSceneByIdAsync(sceneId, cancellationToken);
            if (scene is null)
            {
                return NotFound();
            }

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Scenes.Update)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneResponse>> UpdateScene(int sceneId, [FromBody] UpdateSceneRequest request, CancellationToken cancellationToken)
        {
            var scene = request.MapToScene(sceneId);
            var updated = await _sceneService.UpdateSceneAsync(scene, cancellationToken);
            if (updated is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Scene was not found."
                });
            }

            var response = updated.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = updated.ETag;
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Scenes.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteScene(int sceneId, CancellationToken cancellationToken)
        {
            var deleted = await _sceneService.DeleteSceneAsync(sceneId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Scenes.Share)]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneResponse>> ShareScene(int sceneId, [FromBody] ShareWithGroupRequest request, CancellationToken cancellationToken)
        {
            var shared = await _sceneService.ShareSceneAsync(sceneId, request.GroupId, cancellationToken);
            if (!shared)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "sharing_failed",
                    ErrorDescription = "Scene could not be shared with the group."
                });
            }

            var scene = await _sceneService.GetSceneByIdAsync(sceneId, cancellationToken);
            if (scene is null)
                return NotFound();

            var response = scene.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            Response.Headers.ETag = scene.ETag;
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Scenes.Unshare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveSceneShare(int sceneId, int groupId, CancellationToken cancellationToken)
        {
            var removed = await _sceneService.RemoveSceneShareAsync(sceneId, groupId, cancellationToken);
            if (!removed)
                return NotFound();

            return NoContent();
        }
    }
}
