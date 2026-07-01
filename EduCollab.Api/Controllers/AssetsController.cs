using EduCollab.Api.Mapping;

using EduCollab.Api.Query;

using EduCollab.Api.Requests.Assets;

using EduCollab.Application.Services.Assets;
using EduCollab.Application.Services.Content;

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



        /// <summary>
        /// Create a new asset in the specified group with required ZIP content.
        /// </summary>
        /// <param name="request">Multipart form with asset metadata and ZIP file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Asset was created with content.</response>
        /// <response code="400">Metadata or ZIP file is invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot create assets in this group.</response>
        [Authorize]

        [HttpPost(ApiEndpoints.Assets.Create)]

        [Consumes("multipart/form-data")]

        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]

        public async Task<IActionResult> CreateAsset([FromForm] CreateAssetFormRequest request, CancellationToken cancellationToken)

        {

            if (request.File is null || request.File.Length == 0)

                return ApiBadRequest("invalid_content", "A non-empty ZIP file is required.");



            if (!AssetContentFormats.IsZipContent(request.File.ContentType, request.File.FileName))

                return ApiBadRequest("invalid_content", "Asset content must be a ZIP file.");



            if (string.IsNullOrWhiteSpace(request.Name))

                return ApiBadRequest("invalid_name", "Name is required.");



            var groupIds = ResourceGroupPlacement.ResolveGroupIds(request.GroupId, request.GroupIds);

            var asset = new EduCollab.Application.Models.Asset
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                AssetType = AssetContentFormats.DefaultAssetType,
            };



            await using var stream = request.File.OpenReadStream();

            var created = await _assetService.CreateAssetWithContentAsync(
                asset,
                groupIds,
                request.File.ContentType,
                request.File.FileName,
                stream,
                cancellationToken);

            if (!created)

                return ApiBadRequest("creation_failed", "Asset could not be created.");



            var response = asset.MapToResponse();

            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);

            return CreatedAtAction(nameof(GetAsset), new { assetId = asset.Id }, response);

        }



        /// <summary>
        /// List accessible assets in the current workspace.
        /// </summary>
        /// <param name="owner">Optional filter. Set to <c>me</c> to return only assets owned by the caller.</param>
        /// <param name="sort">Optional sort field (<c>name</c>, <c>createdAt</c>, <c>updatedAt</c>, <c>id</c>). Prefix with <c>-</c> for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Paged list of assets.</response>
        /// <response code="400">Invalid filter, sort, or pagination.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access assets in this workspace.</response>
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



        /// <summary>
        /// Retrieve an asset by id.
        /// </summary>
        /// <param name="assetId">Asset identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the asset.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this asset.</response>
        /// <response code="404">Asset was not found.</response>
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



        /// <summary>
        /// Update asset metadata and group placement.
        /// </summary>
        /// <param name="assetId">Asset identifier.</param>
        /// <param name="request">Asset update payload. Include <c>groupId</c> to move the asset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Asset was updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this asset.</response>
        /// <response code="404">Asset was not found.</response>
        [Authorize]

        [HttpPut(ApiEndpoints.Assets.Update)]

        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<AssetResponse>> UpdateAsset(int assetId, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)

        {

            var asset = request.MapToAsset(assetId);

            var groupIdsToApply = request.GroupIds;
            if (groupIdsToApply is null && request.GroupId > 0)
                groupIdsToApply = new List<int> { request.GroupId };

            var updated = await _assetService.UpdateAssetAsync(asset, groupIdsToApply, cancellationToken);

            if (updated is null)

                return ApiNotFound("update_failed", "Asset was not found.");



            var response = updated.MapToResponse();

            response.CanManage = await _assetService.CanCurrentUserManageAssetAsync(updated.OwnerUserId, cancellationToken);

            return Ok(response);

        }



        /// <summary>
        /// Delete an asset.
        /// </summary>
        /// <param name="assetId">Asset identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Asset was deleted.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot delete this asset.</response>
        /// <response code="404">Asset was not found.</response>
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



        /// <summary>
        /// Download asset binary content (ZIP file).
        /// </summary>
        /// <param name="assetId">Asset identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Asset ZIP content.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this asset.</response>
        /// <response code="404">Asset or content was not found.</response>
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



        /// <summary>
        /// Upload or replace asset binary content (ZIP file).
        /// </summary>
        /// <param name="assetId">Asset identifier.</param>
        /// <param name="file">Non-empty ZIP file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Content was saved.</response>
        /// <response code="400">File is missing, empty, or not a ZIP.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this asset.</response>
        /// <response code="404">Asset was not found.</response>
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


