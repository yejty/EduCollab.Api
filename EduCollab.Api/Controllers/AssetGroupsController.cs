using EduCollab.Application.Services.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AssetGroupsController : ApiControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetGroupsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        /// <summary>
        /// List groups an asset is shared with.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.AssetGroups.GetAll)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> GetAssetGroups(
            [FromQuery] int assetId,
            CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            try
            {
                var groupIds = await _assetService.GetAssetGroupIdsAsync(assetId, cancellationToken);
                return Ok(new ResourceGroupsResponse { GroupIds = groupIds });
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Share an asset with a group.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.AssetGroups.Create)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ResourceGroupsResponse>> AddAssetGroup(
            [FromBody] AttachAssetGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (request.AssetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId must be a positive integer.");

            if (request.GroupId <= 0)
                return ApiBadRequest("invalid_group_id", "groupId must be a positive integer.");

            var added = await _assetService.AddAssetGroupAsync(request.AssetId, request.GroupId, cancellationToken);
            if (!added)
                return ApiNotFound("share_failed", "Asset or group was not found.");

            var groupIds = await _assetService.GetAssetGroupIdsAsync(request.AssetId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ResourceGroupsResponse { GroupIds = groupIds });
        }

        /// <summary>
        /// Replace all group shares for an asset.
        /// </summary>
        [Authorize]
        [HttpPut(ApiEndpoints.AssetGroups.Update)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> SetAssetGroups(
            [FromQuery] int assetId,
            [FromBody] SetResourceGroupsRequest request,
            CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            var groupIds = await _assetService.SetAssetGroupIdsAsync(assetId, request.GroupIds, cancellationToken);
            if (groupIds is null)
                return ApiNotFound("update_failed", "Asset was not found.");

            return Ok(new ResourceGroupsResponse { GroupIds = groupIds });
        }

        /// <summary>
        /// Remove an asset from a group.
        /// </summary>
        [Authorize]
        [HttpDelete(ApiEndpoints.AssetGroups.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveAssetGroup(
            [FromQuery] int assetId,
            [FromQuery] int groupId,
            CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            if (groupId <= 0)
                return ApiBadRequest("invalid_group_id", "groupId is required and must be a positive integer.");

            var removed = await _assetService.RemoveAssetGroupAsync(assetId, groupId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
