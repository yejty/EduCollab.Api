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
    public class AssetFoldersController : ControllerBase
    {
        private readonly IAssetFolderService _assetFolderService;
        private readonly IAssetService _assetService;

        public AssetFoldersController(IAssetFolderService assetFolderService, IAssetService assetService)
        {
            _assetFolderService = assetFolderService;
            _assetService = assetService;
        }

        private async Task PopulateFolderMetadataAsync(AssetFolderResponse folder, CancellationToken cancellationToken)
        {
            folder.CanManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(cancellationToken);
            folder.GroupIds = await _assetFolderService.GetAssetFolderGroupIdsAsync(folder.Id, cancellationToken);
        }

        private async Task PopulateAssetMetadataAsync(AssetResponse asset, CancellationToken cancellationToken)
        {
            asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
            asset.GroupIds = await _assetService.GetAssetGroupIdsAsync(asset.Id, cancellationToken);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.AssetFolders.Create)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAssetFolder([FromBody] CreateAssetFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = request.MapToAssetFolder();
            var created = await _assetFolderService.CreateAssetFolderAsync(folder, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Asset folder could not be created."
                });
            }

            var response = folder.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return CreatedAtAction(nameof(GetAssetFolder), new { folderId = folder.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.GetAll)]
        [ProducesResponseType(typeof(AssetFoldersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetFoldersResponse>> GetRootAssetFolders(CancellationToken cancellationToken)
        {
            var folders = await _assetFolderService.GetRootAssetFoldersAsync(cancellationToken);
            var response = folders.MapToResponse();
            foreach (var folder in response.Folders)
            {
                await PopulateFolderMetadataAsync(folder, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.Get)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> GetAssetFolder(int folderId, CancellationToken cancellationToken)
        {
            var folder = await _assetFolderService.GetAssetFolderByIdAsync(folderId, cancellationToken);
            if (folder is null)
            {
                return NotFound();
            }

            var response = folder.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.AssetFolders.Update)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> UpdateAssetFolder(int folderId, [FromBody] UpdateAssetFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = request.MapToAssetFolder(folderId);
            var updated = await _assetFolderService.UpdateAssetFolderAsync(folder, cancellationToken);
            if (updated is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Asset folder was not found."
                });
            }

            var response = updated.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.AssetFolders.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAssetFolder(int folderId, CancellationToken cancellationToken)
        {
            var deleted = await _assetFolderService.DeleteAssetFolderAsync(folderId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.GetSubFolders)]
        [ProducesResponseType(typeof(AssetFoldersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFoldersResponse>> GetSubFolders(int folderId, CancellationToken cancellationToken)
        {
            var folders = await _assetFolderService.GetSubFoldersAsync(folderId, cancellationToken);
            var response = folders.MapToResponse();
            foreach (var folder in response.Folders)
            {
                await PopulateFolderMetadataAsync(folder, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.GetAssets)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(int folderId, CancellationToken cancellationToken)
        {
            var assets = await _assetService.GetAssetsInFolderAsync(folderId, cancellationToken);
            var response = assets.MapToResponse();

            foreach (var asset in response.Assets)
            {
                await PopulateAssetMetadataAsync(asset, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost(ApiEndpoints.AssetFolders.Share)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> ShareAssetFolder(int folderId, [FromBody] ShareWithGroupRequest request, CancellationToken cancellationToken)
        {
            var shared = await _assetFolderService.ShareAssetFolderAsync(folderId, request.GroupId, request.MapToGroupRole(), cancellationToken);
            if (!shared)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "sharing_failed",
                    ErrorDescription = "Asset folder could not be shared with the group."
                });
            }

            var folder = await _assetFolderService.GetAssetFolderByIdAsync(folderId, cancellationToken);
            if (folder is null)
                return NotFound();

            var response = folder.MapToResponse();
            await PopulateFolderMetadataAsync(response, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.AssetFolders.Unshare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAssetFolderShare(int folderId, int groupId, CancellationToken cancellationToken)
        {
            var removed = await _assetFolderService.RemoveAssetFolderShareAsync(folderId, groupId, cancellationToken);
            if (!removed)
                return NotFound();

            return NoContent();
        }
    }
}
