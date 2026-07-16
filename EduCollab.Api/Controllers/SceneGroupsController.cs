using EduCollab.Api.Swagger;
using EduCollab.Application.Models;
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
        [RequiresWorkspaceParameter("addScenes", Notes = "Also requires effective access to the scene.")]
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
                var shares = await _sceneService.GetSceneGroupSharesAsync(sceneId, cancellationToken);
                return Ok(MapShares(shares));
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
        [RequiresWorkspaceParameter("addScenes", Notes = "Also requires manage access to the scene.")]
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

            var added = await _sceneService.AddSceneGroupAsync(
                request.SceneId,
                request.GroupId,
                request.IncludeAssets,
                cancellationToken);
            if (!added)
                return ApiNotFound("share_failed", "Scene or group was not found.");

            var shares = await _sceneService.GetSceneGroupSharesAsync(request.SceneId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, MapShares(shares));
        }

        /// <summary>
        /// Replace all group shares for a scene.
        /// </summary>
        [Authorize]
        [RequiresWorkspaceParameter("addScenes", Notes = "Also requires manage access to the scene.")]
        [HttpPut(ApiEndpoints.SceneGroups.Update)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> SetSceneGroups(
            [FromQuery] int sceneId,
            [FromBody] SetSceneGroupsRequest request,
            CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            try
            {
                var shares = (request.Groups ?? [])
                    .Select(group => new SceneGroupShare
                    {
                        GroupId = group.GroupId,
                        IncludeAssets = group.IncludeAssets,
                    })
                    .ToList();

                var updated = await _sceneService.SetSceneGroupSharesAsync(sceneId, shares, cancellationToken);
                if (updated is null)
                    return ApiNotFound("update_failed", "Scene was not found.");

                return Ok(MapShares(updated));
            }
            catch (ArgumentException ex) when (ex.ParamName == "groupIds")
            {
                return ApiBadRequest("invalid_group_id", ex.Message);
            }
        }

        /// <summary>
        /// Remove a scene from a group.
        /// </summary>
        [Authorize]
        [RequiresWorkspaceParameter("addScenes", Notes = "Also requires manage access to the scene.")]
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

        private static ResourceGroupsResponse MapShares(IReadOnlyList<SceneGroupShare> shares) =>
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
}
