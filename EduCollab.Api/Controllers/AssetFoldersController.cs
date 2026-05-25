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

        [Authorize]
        [HttpPost(ApiEndpoints.AssetFolders.Create)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAssetFolder(int workspaceId, [FromBody] CreateAssetFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = request.MapToAssetFolder();
            var created = await _assetFolderService.CreateAssetFolderAsync(workspaceId, folder, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Asset folder could not be created."
                });
            }

            var response = folder.MapToResponse();
            response.CanManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(workspaceId, cancellationToken);
            return CreatedAtAction(nameof(GetAssetFolder), new { workspaceId, folderId = folder.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.GetAll)]
        [ProducesResponseType(typeof(AssetFoldersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetFoldersResponse>> GetRootAssetFolders(int workspaceId, CancellationToken cancellationToken)
        {
            var folders = await _assetFolderService.GetRootAssetFoldersAsync(workspaceId, cancellationToken);
            var canManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(workspaceId, cancellationToken);
            var response = folders.MapToResponse();
            foreach (var folder in response.Folders)
            {
                folder.CanManage = canManage;
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.Get)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> GetAssetFolder(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            var folder = await _assetFolderService.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (folder is null)
            {
                return NotFound();
            }

            var response = folder.MapToResponse();
            response.CanManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(workspaceId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.AssetFolders.Update)]
        [ProducesResponseType(typeof(AssetFolderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFolderResponse>> UpdateAssetFolder(int workspaceId, int folderId, [FromBody] UpdateAssetFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = request.MapToAssetFolder(folderId);
            var updated = await _assetFolderService.UpdateAssetFolderAsync(workspaceId, folder, cancellationToken);
            if (updated is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Asset folder was not found."
                });
            }

            var response = updated.MapToResponse();
            response.CanManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(workspaceId, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.AssetFolders.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAssetFolder(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            var deleted = await _assetFolderService.DeleteAssetFolderAsync(workspaceId, folderId, cancellationToken);
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
        public async Task<ActionResult<AssetFoldersResponse>> GetSubFolders(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            var folders = await _assetFolderService.GetSubFoldersAsync(workspaceId, folderId, cancellationToken);
            var canManage = await _assetFolderService.CanCurrentUserManageWorkspaceAssetsAsync(workspaceId, cancellationToken);
            var response = folders.MapToResponse();
            foreach (var folder in response.Folders)
            {
                folder.CanManage = canManage;
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.AssetFolders.GetAssets)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            var assets = await _assetService.GetAssetsInFolderAsync(workspaceId, folderId, cancellationToken);
            var response = assets.MapToResponse();

            foreach (var asset in response.Assets)
            {
                asset.CanManage = await _assetService.CanCurrentUserManageAssetAsync(workspaceId, asset.OwnerUserId, cancellationToken);
            }

            return Ok(response);
        }
    }
}
