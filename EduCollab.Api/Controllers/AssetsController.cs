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

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Create)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAsset(int workspaceId, [FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset();
            var created = await _assetService.CreateAssetAsync(workspaceId, asset, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Asset could not be created."
                });
            }

            var response = asset.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, asset.OwnerUserId, cancellationToken);
            return CreatedAtAction(nameof(GetAsset), new { workspaceId, assetId = asset.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetAll)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(int workspaceId, CancellationToken cancellationToken)
        {
            var assets = await _assetService.GetAllAssetsAsync(workspaceId, cancellationToken);
            var response = assets.MapToResponse();

            foreach (var asset in response.Assets)
            {
                asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, asset.OwnerUserId, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetMine)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetsResponse>> GetMyAssets(int workspaceId, CancellationToken cancellationToken)
        {
            var assets = await _assetService.GetMyAssetsAsync(workspaceId, cancellationToken);
            var response = assets.MapToResponse();

            foreach (var asset in response.Assets)
            {
                asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, asset.OwnerUserId, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.Get)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> GetAsset(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            var asset = await _assetService.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (asset is null)
            {
                return NotFound();
            }

            var response = asset.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, asset.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Assets.Update)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> UpdateAsset(int workspaceId, int assetId, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset(assetId);
            var updated = await _assetService.UpdateAssetAsync(workspaceId, asset, cancellationToken);
            if (updated is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Asset was not found."
                });
            }

            var response = updated.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, updated.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Move)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> MoveAsset(int workspaceId, int assetId, [FromBody] MoveAssetRequest request, CancellationToken cancellationToken)
        {
            var moved = await _assetService.MoveAssetAsync(workspaceId, assetId, request.FolderId, cancellationToken);
            if (moved is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "move_failed",
                    ErrorDescription = "Asset was not found."
                });
            }

            var response = moved.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, moved.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Assets.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsset(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            var deleted = await _assetService.DeleteAssetAsync(workspaceId, assetId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
