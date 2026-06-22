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
    public class AssetFoldersController : ApiControllerBase
    {
        private readonly IAssetFolderService _assetFolderService;
        private readonly IGroupService _groupService;

        public AssetFoldersController(IAssetFolderService assetFolderService, IGroupService groupService)
        {
            _assetFolderService = assetFolderService;
            _groupService = groupService;
        }

        private async Task PopulateFolderMetadataAsync(AssetFolderResponse folder, CancellationToken cancellationToken)
        {
            folder.CanManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(cancellationToken);
            folder.GroupIds = await _assetFolderService.GetAssetFolderGroupIdsAsync(folder.Id, cancellationToken);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.AssetFolders.Create)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAssetFolder([FromBody] CreateAssetFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = request.MapToAssetFolder();
            var created = await _assetFolderService.CreateAssetFolderAsync(folder, request.GroupId, cancellationToken);
            if (!created)
            {
                return ApiBadRequest("creation_failed", "Asset folder could not be created.");
            }

            var response = folder.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return CreatedAtAction(nameof(GetAssetFolder), new { folderId = folder.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.GetAll)]
        [ProducesResponseType(typeof(AssetFoldersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetFoldersResponse>> GetRootAssetFolders(
            [FromQuery] int? groupId,
            [FromQuery] int? parentFolderId,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (!AssetFolderListQueryParser.TryParse(groupId, parentFolderId, out var filter, out var filterError))
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

            List<Application.Models.AssetFolder> folders;
            if (filter.GroupId is int groupIdValue)
            {
                folders = filter.ParentFolderId is int parentFolderIdValue
                    ? await _groupService.GetVisibleSubFoldersAsync(groupIdValue, parentFolderIdValue, cancellationToken)
                    : await _groupService.GetVisibleRootAssetFoldersAsync(groupIdValue, cancellationToken);
            }
            else if (filter.ParentFolderId is int parentFolderIdOnly)
            {
                folders = await _assetFolderService.GetSubFoldersAsync(parentFolderIdOnly, cancellationToken);
            }
            else
            {
                folders = await _assetFolderService.GetRootAssetFoldersAsync(cancellationToken);
            }

            var sorted = ResourceSortProfiles.NamedResource.ApplyAssetFolders(folders, sortSpecification);
            var paged = PaginationApplier.Apply(sorted, paginationSpecification);
            var response = paged.MapToResponse();
            foreach (var folder in response.Folders)
            {
                await PopulateFolderMetadataAsync(folder, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.Get)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> GetAssetFolder(int folderId, CancellationToken cancellationToken)
        {
            var folder = await _assetFolderService.GetAssetFolderByIdAsync(folderId, cancellationToken);
            if (folder is null)
            {
                return ApiNotFound();
            }

            var response = folder.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.AssetFolders.Update)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> UpdateAssetFolder(int folderId, [FromBody] UpdateAssetFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = request.MapToAssetFolder(folderId);
            var updated = await _assetFolderService.UpdateAssetFolderAsync(folder, cancellationToken);
            if (updated is null)
            {
                return ApiNotFound("update_failed", "Asset folder was not found.");
            }

            var response = updated.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.AssetFolders.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAssetFolder(int folderId, CancellationToken cancellationToken)
        {
            var deleted = await _assetFolderService.DeleteAssetFolderAsync(folderId, cancellationToken);
            if (!deleted)
            {
                return ApiNotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpPost(ApiEndpoints.AssetFolders.Share)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> ShareAssetFolder(int folderId, [FromBody] ShareWithGroupRequest request, CancellationToken cancellationToken)
        {
            var shared = await _assetFolderService.ShareAssetFolderAsync(folderId, request.GroupId, cancellationToken);
            if (!shared)
            {
                return ApiBadRequest("sharing_failed", "Asset folder could not be shared with the group.");
            }

            var folder = await _assetFolderService.GetAssetFolderByIdAsync(folderId, cancellationToken);
            if (folder is null)
                return ApiNotFound();

            var response = folder.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.AssetFolders.Unshare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAssetFolderShare(int folderId, int groupId, CancellationToken cancellationToken)
        {
            var removed = await _assetFolderService.RemoveAssetFolderShareAsync(folderId, groupId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
