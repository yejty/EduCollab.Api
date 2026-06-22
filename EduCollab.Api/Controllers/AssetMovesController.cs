using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Assets;
using EduCollab.Application.Services.Groups;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AssetMovesController : ApiControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetMovesController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        private async Task PopulateAccessMetadataAsync(AssetResponse asset, CancellationToken cancellationToken)
        {
            asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
            asset.GroupIds = await _assetService.GetAssetGroupIdsAsync(asset.Id, cancellationToken);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.AssetMoves.Create)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> MoveAsset([FromBody] CreateAssetMoveRequest request, CancellationToken cancellationToken)
        {
            if (request.AssetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId must be a positive integer.");

            var moved = await _assetService.MoveAssetAsync(request.AssetId, request.FolderId, cancellationToken);
            if (moved is null)
                return ApiNotFound("move_failed", "Asset was not found.");

            var response = moved.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }
    }
}
