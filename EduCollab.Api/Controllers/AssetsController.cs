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
    public class AssetsController : ApiControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly IGroupService _groupService;

        public AssetsController(IAssetService assetService, IGroupService groupService)
        {
            _assetService = assetService;
            _groupService = groupService;
        }

        private async Task PopulateAccessMetadataAsync(AssetResponse asset, CancellationToken cancellationToken)
        {
            asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
            asset.GroupIds = await _assetService.GetAssetGroupIdsAsync(asset.Id, cancellationToken);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Create)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset();
            var created = await _assetService.CreateAssetAsync(asset, request.GroupId, cancellationToken);
            if (!created)
            {
                return ApiBadRequest("creation_failed", "Asset could not be created.");
            }

            var response = asset.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return CreatedAtAction(nameof(GetAsset), new { assetId = asset.Id }, response);
        }

        private async Task<ActionResult<AssetsResponse>> BuildAssetsResponseAsync(
            List<Application.Models.Asset> assets,
            SortSpecification sortSpecification,
            PaginationSpecification paginationSpecification,
            CancellationToken cancellationToken)
        {
            var sorted = ResourceSortProfiles.NamedResource.ApplyAssets(assets, sortSpecification);
            var paged = PaginationApplier.Apply(sorted, paginationSpecification);
            var response = paged.MapToResponse();

            foreach (var asset in response.Assets)
            {
                await PopulateAccessMetadataAsync(asset, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetAll)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(
            [FromQuery] string? owner,
            [FromQuery] int? folderId,
            [FromQuery] int? groupId,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (!AssetListQueryParser.TryParse(owner, folderId, groupId, out var filter, out var filterError))
                return ApiBadRequest("invalid_filter", filterError!);

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

            List<Application.Models.Asset> assets;
            if (filter.OwnerIsCurrentUser)
            {
                assets = await _assetService.GetMyAssetsAsync(cancellationToken);
            }
            else if (filter.GroupId is int groupIdValue)
            {
                assets = filter.FolderId is int folderIdValue
                    ? await _groupService.GetVisibleAssetsInFolderAsync(groupIdValue, folderIdValue, cancellationToken)
                    : await _groupService.GetVisibleRootAssetsAsync(groupIdValue, cancellationToken);
            }
            else if (filter.FolderId is int folderIdOnly)
            {
                assets = await _assetService.GetAssetsInFolderAsync(folderIdOnly, cancellationToken);
            }
            else
            {
                assets = await _assetService.GetAllAssetsAsync(cancellationToken);
            }

            return await BuildAssetsResponseAsync(assets, sortSpecification, paginationSpecification, cancellationToken);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.Get)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> GetAsset(int assetId, CancellationToken cancellationToken)
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
            {
                return ApiNotFound();
            }

            var response = asset.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Assets.Update)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> UpdateAsset(int assetId, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
        {
            var asset = request.MapToAsset(assetId);
            var updated = await _assetService.UpdateAssetAsync(asset, cancellationToken);
            if (updated is null)
            {
                return ApiNotFound("update_failed", "Asset was not found.");
            }

            var response = updated.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Assets.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsset(int assetId, CancellationToken cancellationToken)
        {
            var deleted = await _assetService.DeleteAssetAsync(assetId, cancellationToken);
            if (!deleted)
            {
                return ApiNotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.Content)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAssetContent(int assetId, [FromQuery] int? versionNumber, CancellationToken cancellationToken)
        {
            var content = await _assetService.GetAssetContentAsync(assetId, versionNumber, cancellationToken);
            if (content is null)
            {
                return ApiNotFound();
            }

            return File(content.Data, content.ContentType);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Assets.Content)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutAssetContent(int assetId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return ApiBadRequest("invalid_content", "A non-empty file is required.");
            }

            await using var stream = file.OpenReadStream();
            await _assetService.SaveAssetContentAsync(assetId, file.ContentType, file.FileName, stream, cancellationToken);
            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetVersions)]
        [ProducesResponseType(typeof(AssetVersionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetVersionsResponse>> GetAssetVersions(int assetId, CancellationToken cancellationToken)
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
                return ApiNotFound();

            var versions = await _assetService.GetAssetVersionsAsync(assetId, cancellationToken);
            return Ok(versions.MapToResponse());
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetVersion)]
        [ProducesResponseType(typeof(AssetVersionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetVersionResponse>> GetAssetVersion(int assetId, int versionNumber, CancellationToken cancellationToken)
        {
            var version = await _assetService.GetAssetVersionAsync(assetId, versionNumber, cancellationToken);
            if (version is null)
                return ApiNotFound();

            return Ok(version.MapToResponse());
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Assets.GetVersionContent)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAssetVersionContent(int assetId, int versionNumber, CancellationToken cancellationToken)
        {
            var content = await _assetService.GetAssetContentAsync(assetId, versionNumber, cancellationToken);
            if (content is null)
                return ApiNotFound();

            return File(content.Data, content.ContentType);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Assets.Share)]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponse>> ShareAsset(int assetId, [FromBody] ShareWithGroupRequest request, CancellationToken cancellationToken)
        {
            var shared = await _assetService.ShareAssetAsync(assetId, request.GroupId, cancellationToken);
            if (!shared)
            {
                return ApiBadRequest("sharing_failed", "Asset could not be shared with the group.");
            }

            var asset = await _assetService.GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
                return ApiNotFound();

            var response = asset.MapToResponse();
            await PopulateAccessMetadataAsync(response, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Assets.Unshare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAssetShare(int assetId, int groupId, CancellationToken cancellationToken)
        {
            var removed = await _assetService.RemoveAssetShareAsync(assetId, groupId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
