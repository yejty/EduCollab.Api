using EduCollab.Application.Services.Scenes;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class SceneGroupsController : ApiControllerBase
    {
        private readonly ISceneService _sceneService;

        public SceneGroupsController(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        /// <summary>
        /// List groups a scene is shared with.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.SceneGroups.GetAll)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> GetSceneGroups(
            [FromQuery] int sceneId,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            try
            {
                var groupIds = await _sceneService.GetSceneGroupIdsAsync(sceneId, cancellationToken);
                return Ok(new ResourceGroupsResponse { GroupIds = groupIds });
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Share a scene with a group.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.SceneGroups.Create)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ResourceGroupsResponse>> AddSceneGroup(
            [FromBody] AttachSceneGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (request.SceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId must be a positive integer.");

            if (request.GroupId <= 0)
                return ApiBadRequest("invalid_group_id", "groupId must be a positive integer.");

            var added = await _sceneService.AddSceneGroupAsync(request.SceneId, request.GroupId, cancellationToken);
            if (!added)
                return ApiNotFound("share_failed", "Scene or group was not found.");

            var groupIds = await _sceneService.GetSceneGroupIdsAsync(request.SceneId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ResourceGroupsResponse { GroupIds = groupIds });
        }

        /// <summary>
        /// Replace all group shares for a scene.
        /// </summary>
        [Authorize]
        [HttpPut(ApiEndpoints.SceneGroups.Update)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> SetSceneGroups(
            [FromQuery] int sceneId,
            [FromBody] SetResourceGroupsRequest request,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            var groupIds = await _sceneService.SetSceneGroupIdsAsync(sceneId, request.GroupIds, cancellationToken);
            if (groupIds is null)
                return ApiNotFound("update_failed", "Scene was not found.");

            return Ok(new ResourceGroupsResponse { GroupIds = groupIds });
        }

        /// <summary>
        /// Remove a scene from a group.
        /// </summary>
        [Authorize]
        [HttpDelete(ApiEndpoints.SceneGroups.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveSceneGroup(
            [FromQuery] int sceneId,
            [FromQuery] int groupId,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            if (groupId <= 0)
                return ApiBadRequest("invalid_group_id", "groupId is required and must be a positive integer.");

            var removed = await _sceneService.RemoveSceneGroupAsync(sceneId, groupId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
