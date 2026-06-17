using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Assets;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Workspaces;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IWorkspaceService _workspaceService;
        private readonly IAssetFolderService _assetFolderService;
        private readonly IAssetService _assetService;

        public GroupsController(
            IGroupService groupService,
            IWorkspaceService workspaceService,
            IAssetFolderService assetFolderService,
            IAssetService assetService)
        {
            _groupService = groupService;
            _workspaceService = workspaceService;
            _assetFolderService = assetFolderService;
            _assetService = assetService;
        }

        private async Task<string> GetCurrentUserWorkspaceRoleAsync(CancellationToken cancellationToken)
        {
            var role = await _groupService.GetCurrentUserWorkspaceRoleAsync(cancellationToken);
            return role.ToString();
        }

        private async Task<IReadOnlyDictionary<int, string>> GetWorkspaceRolesByUserIdAsync(CancellationToken cancellationToken)
        {
            var members = await _workspaceService.GetCurrentWorkspaceMembersAsync(cancellationToken);
            return members.ToDictionary(m => m.UserId, m => m.Role.ToString());
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
        [HttpPost(ApiEndpoints.Groups.Create)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var group = request.MapToGroup();

            var created = await _groupService.CreateGroupAsync(group, cancellationToken);
            if (!created)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "creation_failed",
                    ErrorDescription = "Group could not be created."
                });
            }
            var response = group.MapToResponse();
            response.CurrentUserRole = await GetCurrentUserWorkspaceRoleAsync(cancellationToken);
            return CreatedAtAction(nameof(GetGroup), new { groupId = group.Id }, response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAll)]
        [ProducesResponseType(typeof(GroupsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GroupsResponse>> GetAllGroups(CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetAllGroupsAsync(cancellationToken);
            var response = groups.MapToResponse(); 
            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.Get)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupResponse>> GetGroup(int groupId, CancellationToken cancellationToken)
        {
            var group = await _groupService.GetGroupByIdAsync(groupId, cancellationToken);
            if (group is null)
            {
                return NotFound();
            }
            var response = group.MapToResponse();
            response.CurrentUserRole = await GetCurrentUserWorkspaceRoleAsync(cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut(ApiEndpoints.Groups.Update)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupResponse>> UpdateGroup(int groupId, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)
        {
            var group = request.MapToGroup(groupId);
            var updatedGroup = await _groupService.UpdateGroupAsync(group, cancellationToken);
            if (updatedGroup is null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "update_failed",
                    ErrorDescription = "Group was not found.",
                });
            }
            var response = updatedGroup.MapToResponse();
            response.CurrentUserRole = await GetCurrentUserWorkspaceRoleAsync(cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Groups.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteGroup(int groupId, CancellationToken cancellationToken)
        {
            var deleted = await _groupService.DeleteGroupAsync(groupId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAllMembers)]
        [ProducesResponseType(typeof(GroupMembersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GroupMembersResponse>> GetAllMembers(int groupId, CancellationToken cancellationToken)
        {
            var members = await _groupService.GetAllGroupMembersAsync(groupId, cancellationToken);
            var rolesByUserId = await GetWorkspaceRolesByUserIdAsync(cancellationToken);
            return Ok(members.MapToResponse(rolesByUserId));
        }

        [Authorize]
        [HttpPost(ApiEndpoints.Groups.CreateMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GroupMemberResponse>> CreateMember(int groupId, [FromBody] CreateGroupMemberRequest request, CancellationToken cancellationToken)
        {
            var member = request.MapToGroupMember(groupId);
            var created = await _groupService.CreateGroupMemberAsync(groupId, member, cancellationToken);
            if (created is null)
                return BadRequest(new ErrorResponse { Error = "creation_failed", ErrorDescription = "Group member could not be created." });

            var workspaceMember = await _workspaceService.GetCurrentWorkspaceMemberAsync(created.UserId, cancellationToken);
            var role = workspaceMember?.Role.ToString() ?? string.Empty;
            return CreatedAtAction(nameof(GetMember), new { groupId, userId = created.UserId }, created.MapToResponse(role));
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetMember)]
        [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupMemberResponse>> GetMember(int groupId, int userId, CancellationToken cancellationToken)
        {
            var member = await _groupService.GetGroupMemberAsync(groupId, userId, cancellationToken);
            if (member is null)
                return NotFound();

            var workspaceMember = await _workspaceService.GetCurrentWorkspaceMemberAsync(userId, cancellationToken);
            var role = workspaceMember?.Role.ToString() ?? string.Empty;
            return Ok(member.MapToResponse(role));
        }

        [Authorize]
        [HttpDelete(ApiEndpoints.Groups.DeleteMember)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMember(int groupId, int userId, CancellationToken cancellationToken)
        {
            var deleted = await _groupService.DeleteGroupMemberAsync(groupId, userId, cancellationToken);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetFolders)]
        [ProducesResponseType(typeof(AssetFoldersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFoldersResponse>> GetFolders(int groupId, CancellationToken cancellationToken)
        {
            var folders = await _groupService.GetVisibleRootAssetFoldersAsync(groupId, cancellationToken);
            var response = folders.MapToResponse();
            foreach (var folder in response.Folders)
            {
                await PopulateFolderMetadataAsync(folder, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetSubFolders)]
        [ProducesResponseType(typeof(AssetFoldersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetFoldersResponse>> GetSubFolders(int groupId, int folderId, CancellationToken cancellationToken)
        {
            var folders = await _groupService.GetVisibleSubFoldersAsync(groupId, folderId, cancellationToken);
            var response = folders.MapToResponse();
            foreach (var folder in response.Folders)
            {
                await PopulateFolderMetadataAsync(folder, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAssetsInFolders)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetsResponse>> GetAssetsInFolders(int groupId, int folderId, CancellationToken cancellationToken)
        {
            var assets = await _groupService.GetVisibleAssetsInFolderAsync(groupId, folderId, cancellationToken);
            var response = assets.MapToResponse();
            foreach (var asset in response.Assets)
            {
                await PopulateAssetMetadataAsync(asset, cancellationToken);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet(ApiEndpoints.Groups.GetAssets)]
        [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetsResponse>> GetAssets(int groupId, CancellationToken cancellationToken)
        {
            var assets = await _groupService.GetVisibleRootAssetsAsync(groupId, cancellationToken);
            var response = assets.MapToResponse();
            foreach (var asset in response.Assets)
            {
                await PopulateAssetMetadataAsync(asset, cancellationToken);
            }

            return Ok(response);
        }
    }
}
