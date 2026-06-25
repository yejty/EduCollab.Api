using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Assets;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class AssetsController : ApiControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Create)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset();
            var created = await _assetService.CreateAssetAsync(asset, request.GroupId, cancellationToken);
            if (!created)
                return ApiBadRequest("creation_failed", "Asset could not be created.");

            var response = asset.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
            return CreatedAtAction(nameof(GetAsset), new { assetId = asset.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetAll)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(
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

            var assets = ownerIsCurrentUser
                ? await _assetService.GetMyAssetsAsync(cancellationToken)
                : await _assetService.GetAllAssetsAsync(cancellationToken);

            var sorted = ResourceSortProfiles.NamedResource.ApplyAssets(assets, sortSpecification);
            var paged = PaginationApplier.Apply(sorted, paginationSpecification);
            var response = paged.MapToResponse();

            foreach (var asset in response.Assets)
                asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.Get)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AssetResponse>> GetAsset(int assetId, CancellationToken cancellationToken)
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
                return ApiNotFound();

            var response = asset.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Assets.Update)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AssetResponse>> UpdateAsset(int assetId, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset(assetId);
            var updated = await _assetService.UpdateAssetAsync(asset, cancellationToken);
            if (updated is null)
                return ApiNotFound("update_failed", "Asset was not found.");

            var response = updated.MapToResponse();
            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(updated.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Assets.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsset(int assetId, CancellationToken cancellationToken)
        {
            var deleted = await _assetService.DeleteAssetAsync(assetId, cancellationToken);
            if (!deleted)
                return ApiNotFound();

            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.Content)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssetContent(int assetId, CancellationToken cancellationToken)
        {
            var content = await _assetService.GetAssetContentAsync(assetId, cancellationToken);
            if (content is null)
                return ApiNotFound();

            return File(content.Data, content.ContentType);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Assets.Content)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PutAssetContent(int assetId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return ApiBadRequest("invalid_content", "A non-empty ZIP file is required.");

            if (!AssetContentFormats.IsZipContent(file.ContentType, file.FileName))
                return ApiBadRequest("invalid_content", "Asset content must be a ZIP file.");

            await using var stream = file.OpenReadStream();
            await _assetService.SaveAssetContentAsync(assetId, file.ContentType, file.FileName, stream, cancellationToken);
            return NoContent();
        }
    }
}
