using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Assets;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        private async Task PopulateAccessMetadataAsync(AssetResponse asset, CancellationToken cancellationToken)
        {
            asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
            asset.GroupIds = await _assetService.GetAssetGroupIdsAsync(asset.Id, cancellationToken);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Create)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset();
            var created = await _assetService.CreateAssetAsync(asset, request.GroupId, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Asset could not be created."
                });
            }

            var response = asset.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return CreatedAtAction(nameof(GetAsset), new { assetId = asset.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetAll)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(CancellationToken cancellationToken)
        {
            var assets = await _assetService.GetAllAssetsAsync(cancellationToken);
            var response = assets.MapToResponse();

            foreach (var asset in response.Assets)
            {
                await PopulateAccessMetadataAsync(asset, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetMine)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetsResponse>> GetMyAssets(CancellationToken cancellationToken)
        {
            var assets = await _assetService.GetMyAssetsAsync(cancellationToken);
            var response = assets.MapToResponse();

            foreach (var asset in response.Assets)
            {
                await PopulateAccessMetadataAsync(asset, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.Get)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> GetAsset(int assetId, CancellationToken cancellationToken)
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
            {
                return NotFound();
            }

            var response = asset.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Assets.Update)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> UpdateAsset(int assetId, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset(assetId);
            var updated = await _assetService.UpdateAssetAsync(asset, cancellationToken);
            if (updated is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Asset was not found."
                });
            }

            var response = updated.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Move)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> MoveAsset(int assetId, [FromBody] MoveAssetRequest request, CancellationToken cancellationToken)
        {
            var moved = await _assetService.MoveAssetAsync(assetId, request.FolderId, cancellationToken);
            if (moved is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "move_failed",
                    ErrorDescription = "Asset was not found."
                });
            }

            var response = moved.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Assets.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsset(int assetId, CancellationToken cancellationToken)
        {
            var deleted = await _assetService.DeleteAssetAsync(assetId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Share)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> ShareAsset(int assetId, [FromBody] ShareWithGroupRequest request, CancellationToken cancellationToken)
        {
            var shared = await _assetService.ShareAssetAsync(assetId, request.GroupId, cancellationToken);
            if (!shared)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "sharing_failed",
                    ErrorDescription = "Asset could not be shared with the group."
                });
            }

            var asset = await _assetService.GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
                return NotFound();

            var response = asset.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Assets.Unshare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAssetShare(int assetId, int groupId, CancellationToken cancellationToken)
        {
            var removed = await _assetService.RemoveAssetShareAsync(assetId, groupId, cancellationToken);
            if (!removed)
                return NotFound();

            return NoContent();
        }
    }
}
