using EduCollab.Api.Mapping;

using EduCollab.Api.Query;

using EduCollab.Application.Services.Assets;

using EduCollab.Contracts.Requests.Assets;

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



        [Authorize]

        [HttpPost(ApiEndpoints.AssetMoves.Create)]

        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<AssetResponse>> MoveAsset([FromBody] CreateAssetMoveRequest request, CancellationToken cancellationToken)

        {

            if (request.AssetId <= 0)

                return ApiBadRequest("invalid_asset_id", "assetId must be a positive integer.");

            if (request.GroupId <= 0)

                return ApiBadRequest("invalid_group_id", "groupId must be a positive integer.");



            var moved = await _assetService.MoveAssetAsync(request.AssetId, request.GroupId, cancellationToken);

            if (moved is null)

                return ApiNotFound("move_failed", "Asset was not found.");



            var response = moved.MapToResponse();

            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(moved.OwnerUserId, cancellationToken);

            return Ok(response);

        }

    }

}


