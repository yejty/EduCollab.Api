using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
namespace EduCollab.Application.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetFolderRepository _assetFolderRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public AssetService(
            IAssetRepository assetRepository,
            IAssetFolderRepository assetFolderRepository,
            IGroupRepository groupRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _assetRepository = assetRepository;
            _assetFolderRepository = assetFolderRepository;
            _groupRepository = groupRepository;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private static string RequireTrimmed(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} is required.", paramName);

            return value.Trim();
        }

        private async Task<(int WorkspaceId, WorkspaceMember Membership)> RequireWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
            if (user?.WorkspaceId is not int workspaceId || workspaceId <= 0)
                throw new AccessDeniedException("You must belong to a workspace to access assets.");

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                throw new KeyNotFoundException("Workspace not found.");

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
            if (membership is null)
                throw new AccessDeniedException("You are not a member of this workspace.");

            return (workspaceId, membership);
        }

        private static void EnsureCanCreateAsset(WorkspaceMember membership)
        {
            if (!WorkspaceRolePermissions.CanCrudAssets(membership.Role))
                throw new AccessDeniedException("You do not have permission to create assets.");
        }

        private async Task<bool> IsAssetInSharedFolderAsync(int workspaceId, Asset asset, int userId, CancellationToken cancellationToken)
        {
            if (asset.FolderId is not int folderId)
                return false;

            var userGroupIds = await _groupRepository.GetUserGroupIdsAsync(workspaceId, userId, cancellationToken);
            if (userGroupIds.Count == 0)
                return false;

            foreach (var groupId in userGroupIds)
            {
                var folderShares = await _assetFolderRepository.GetAssetFolderSharesByGroupAsync(workspaceId, groupId, cancellationToken);
                if (folderShares.Any(share => share.FolderId == folderId))
                    return true;
            }

            return false;
        }

        private async Task<bool> IsAssetSharedWithUserGroupsAsync(int workspaceId, int assetId, int userId, CancellationToken cancellationToken)
        {
            var userGroupIds = await _groupRepository.GetUserGroupIdsAsync(workspaceId, userId, cancellationToken);
            if (userGroupIds.Count == 0)
                return false;

            var shares = await _assetRepository.GetAssetSharesAsync(workspaceId, assetId, cancellationToken);
            return shares.Any(share => userGroupIds.Contains(share.GroupId));
        }

        private async Task EnsureCanManageAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return;

            if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
                throw new AccessDeniedException("Viewers have read-only access to assets.");

            if (membership.Role == WorkspaceRole.Creator)
                return;

            if (membership.Role == WorkspaceRole.Manager)
            {
                if (asset.OwnerUserId == userId
                    || await IsAssetSharedWithUserGroupsAsync(workspaceId, asset.Id, userId, cancellationToken)
                    || await IsAssetInSharedFolderAsync(workspaceId, asset, userId, cancellationToken))
                {
                    return;
                }
            }

            if (asset.OwnerUserId == userId)
                return;

            throw new AccessDeniedException("You do not have permission to manage this asset.");
        }

        private async Task EnsureCanShareWithGroupOnCreateAsync(int workspaceId, int groupId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanShareContent(membership.Role))
                return;

            var groupMember = await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
            if (groupMember is null)
                throw new AccessDeniedException("You must belong to the selected group to share with it.");
        }

        private async Task<List<AssetFolder>> GetAllFoldersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            var rootFolders = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, null, cancellationToken);
            var result = new List<AssetFolder>(rootFolders);
            var queue = new Queue<AssetFolder>(rootFolders);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, current.Id, cancellationToken);
                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child);
                }
            }

            return result;
        }

        private sealed record AssetVisibilityContext(
            bool CanSeeAllContent,
            int UserId,
            HashSet<int> UserGroupIds,
            HashSet<int> VisibleFolderIds,
            HashSet<int> DirectlySharedAssetIds);

        private async Task<AssetVisibilityContext> BuildAssetVisibilityContextAsync(
            int workspaceId,
            WorkspaceMember membership,
            int userId,
            CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return new AssetVisibilityContext(true, userId, [], [], []);

            var userGroupIds = (await _groupRepository.GetUserGroupIdsAsync(workspaceId, userId, cancellationToken)).ToHashSet();
            var folders = await GetAllFoldersAsync(workspaceId, cancellationToken);
            var folderShares = await _assetFolderRepository.GetWorkspaceAssetFolderSharesAsync(workspaceId, cancellationToken);
            var visibleFolderIds = WorkspaceContentVisibility.BuildVisibleFolderIds(folders, folderShares, userGroupIds);
            var assetShares = await _assetRepository.GetWorkspaceAssetSharesAsync(workspaceId, cancellationToken);
            var directlySharedAssetIds = assetShares
                .Where(share => userGroupIds.Contains(share.GroupId))
                .Select(share => share.AssetId)
                .ToHashSet();

            return new AssetVisibilityContext(false, userId, userGroupIds, visibleFolderIds, directlySharedAssetIds);
        }

        private static bool IsAssetVisible(Asset asset, AssetVisibilityContext context) =>
            WorkspaceContentVisibility.IsAssetVisibleToUser(
                asset,
                context.UserId,
                context.CanSeeAllContent,
                context.UserGroupIds,
                context.DirectlySharedAssetIds,
                context.VisibleFolderIds);

        private async Task EnsureFolderBelongsToWorkspaceAsync(int workspaceId, int? folderId, CancellationToken cancellationToken)
        {
            if (folderId is null)
                return;

            var folder = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId.Value, cancellationToken);
            if (folder is null)
                throw new KeyNotFoundException("Asset folder not found.");
        }

        private async Task EnsureGroupBelongsToWorkspaceAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");
        }

        public async Task<bool> CreateAssetAsync(Asset asset, int? groupId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(asset);

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanCreateAsset(membership);
            var userId = RequireCurrentUserId();

            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, asset.FolderId, cancellationToken);

            asset.WorkspaceId = workspaceId;
            asset.OwnerUserId = userId;
            asset.Name = RequireTrimmed(asset.Name, nameof(asset.Name));
            asset.Description = string.IsNullOrWhiteSpace(asset.Description) ? null : asset.Description.Trim();
            asset.AssetType = RequireTrimmed(asset.AssetType, nameof(asset.AssetType));
            asset.StorageUrl = RequireTrimmed(asset.StorageUrl, nameof(asset.StorageUrl));
            asset.Version = string.IsNullOrWhiteSpace(asset.Version) ? null : asset.Version.Trim();
            asset.CreatedAtUtc = DateTime.UtcNow;
            asset.UpdatedAtUtc = asset.CreatedAtUtc;

            var id = await _assetRepository.CreateAssetAsync(workspaceId, asset, cancellationToken);
            if (id <= 0)
                return false;

            asset.Id = id;

            if (groupId is int selectedGroupId)
            {
                await EnsureGroupBelongsToWorkspaceAsync(workspaceId, selectedGroupId, cancellationToken);
                await EnsureCanShareWithGroupOnCreateAsync(workspaceId, selectedGroupId, membership, userId, cancellationToken);

                var share = new AssetGroupShare
                {
                    AssetId = id,
                    GroupId = selectedGroupId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _assetRepository.CreateAssetShareAsync(workspaceId, share, cancellationToken);
            }

            return true;
        }

        public async Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var assets = await _assetRepository.GetAllAssetsAsync(workspaceId, cancellationToken);
            var context = await BuildAssetVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return assets.Where(asset => IsAssetVisible(asset, context)).ToList();
        }

        public async Task<List<Asset>> GetAssetsInFolderAsync(int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, folderId, cancellationToken);
            var userId = RequireCurrentUserId();
            var assets = await _assetRepository.GetAssetsByFolderAsync(workspaceId, folderId, cancellationToken);
            var context = await BuildAssetVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return assets.Where(asset => IsAssetVisible(asset, context)).ToList();
        }

        public async Task<List<Asset>> GetMyAssetsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            return await _assetRepository.GetAssetsByOwnerAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<Asset?> GetAssetByIdAsync(int assetId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var asset = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (asset is null)
                return null;

            var userId = RequireCurrentUserId();
            var context = await BuildAssetVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return IsAssetVisible(asset, context) ? asset : null;
        }

        public async Task<Asset?> UpdateAssetAsync(Asset asset, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(asset);
            if (asset.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(asset.Id));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, asset.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, asset.FolderId, cancellationToken);

            existing.Name = RequireTrimmed(asset.Name, nameof(asset.Name));
            existing.Description = string.IsNullOrWhiteSpace(asset.Description) ? null : asset.Description.Trim();
            existing.FolderId = asset.FolderId;
            existing.AssetType = RequireTrimmed(asset.AssetType, nameof(asset.AssetType));
            existing.StorageUrl = RequireTrimmed(asset.StorageUrl, nameof(asset.StorageUrl));
            existing.Version = string.IsNullOrWhiteSpace(asset.Version) ? null : asset.Version.Trim();

            return await _assetRepository.UpdateAssetAsync(workspaceId, existing, cancellationToken);
        }

        public async Task<Asset?> MoveAssetAsync(int assetId, int? folderId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, folderId, cancellationToken);

            return await _assetRepository.MoveAssetAsync(workspaceId, assetId, folderId, cancellationToken);
        }

        public async Task<bool> DeleteAssetAsync(int assetId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            return await _assetRepository.DeleteAssetAsync(workspaceId, assetId, cancellationToken);
        }

        public async Task<bool> ShareAssetAsync(int assetId, int groupId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return false;

            var userId = RequireCurrentUserId();
            if (!WorkspaceRolePermissions.CanShareContent(membership.Role) && existing.OwnerUserId != userId)
                throw new AccessDeniedException("Only workspace owners and managers can share assets with groups.");

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);

            var share = new AssetGroupShare
            {
                AssetId = assetId,
                GroupId = groupId,
                CreatedByUserId = RequireCurrentUserId(),
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _assetRepository.CreateAssetShareAsync(workspaceId, share, cancellationToken);
            return created is not null;
        }

        public async Task<bool> RemoveAssetShareAsync(int assetId, int groupId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            return await _assetRepository.DeleteAssetShareAsync(workspaceId, assetId, groupId, cancellationToken);
        }

        public async Task<bool> CanCurrentUserManageAssetAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            try
            {
                var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
                var userId = RequireCurrentUserId();

                if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                    return true;

                if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
                    return false;

                if (membership.Role == WorkspaceRole.Creator)
                    return true;

                if (userId == ownerUserId)
                    return true;

                return membership.Role == WorkspaceRole.Manager;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (AccessDeniedException)
            {
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public async Task<List<int>> GetAssetGroupIdsAsync(int assetId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var shares = await _assetRepository.GetAssetSharesAsync(workspaceId, assetId, cancellationToken);
            return shares.Select(s => s.GroupId).ToList();
        }
    }
}
