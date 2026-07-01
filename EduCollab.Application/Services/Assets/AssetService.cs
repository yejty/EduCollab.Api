using EduCollab.Application.Exceptions;

using EduCollab.Application.Identity;

using EduCollab.Application.Models;

using EduCollab.Application.Repositories;

using EduCollab.Application.Services.Content;

using EduCollab.Application.Services.Groups;

using EduCollab.Application.Services.Workspaces;

using Microsoft.Extensions.Options;



namespace EduCollab.Application.Services.Assets

{

    public class AssetService : IAssetService

    {

        private readonly IAssetRepository _assetRepository;

        private readonly IAssetContentStore _assetContentStore;

        private readonly IGroupRepository _groupRepository;

        private readonly IGroupAccessResolver _groupAccessResolver;

        private readonly IWorkspaceRepository _workspaceRepository;

        private readonly IUserRepository _userRepository;

        private readonly ICurrentUser _currentUser;

        private readonly long _maxAssetBytes;



        public AssetService(

            IAssetRepository assetRepository,

            IAssetContentStore assetContentStore,

            IGroupRepository groupRepository,

            IGroupAccessResolver groupAccessResolver,

            IWorkspaceRepository workspaceRepository,

            IUserRepository userRepository,

            ICurrentUser currentUser,

            IOptions<WorkspaceContentStorageOptions> contentStorageOptions)

        {

            _assetRepository = assetRepository;

            _assetContentStore = assetContentStore;

            _groupRepository = groupRepository;

            _groupAccessResolver = groupAccessResolver;

            _workspaceRepository = workspaceRepository;

            _userRepository = userRepository;

            _currentUser = currentUser;

            _maxAssetBytes = contentStorageOptions.Value.MaxAssetBytes;

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



        private Task<(int WorkspaceId, WorkspaceMember Membership)> RequireWorkspaceMembershipAsync(CancellationToken cancellationToken)

        {

            var userId = RequireCurrentUserId();

            return CurrentWorkspaceAccess.RequireMembershipAsync(

                _userRepository,

                _workspaceRepository,

                userId,

                cancellationToken);

        }



        private static void EnsureCanCreateAsset(WorkspaceMember membership)

        {

            if (!WorkspaceRolePermissions.CanCrudAssets(membership.Role))

                throw new AccessDeniedException("You do not have permission to create assets.");

        }



        private async Task<HashSet<int>> GetAccessibleGroupIdsAsync(int workspaceId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)

        {

            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))

            {

                var allGroups = await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);

                return allGroups.Select(g => g.Id).ToHashSet();

            }



            return await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(workspaceId, userId, cancellationToken);

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



            if (asset.OwnerUserId == userId)

                return;



            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);

            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, asset, cancellationToken);

            if (membership.Role == WorkspaceRole.Manager
                && ContentGroupShareOperations.ManagerCanManageViaGroups(
                    membership,
                    asset.OwnerUserId,
                    userId,
                    asset.GroupIds,
                    asset.GroupId,
                    accessibleGroupIds))
                return;



            throw new AccessDeniedException("You do not have permission to manage this asset.");

        }



        private async Task EnsureGroupBelongsToWorkspaceAsync(int workspaceId, int groupId, CancellationToken cancellationToken)

        {

            if (groupId <= 0)

                throw new ArgumentOutOfRangeException(nameof(groupId));



            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);

            if (group is null)

                throw new KeyNotFoundException("Group not found.");

        }



        private async Task EnsureCanPlaceInGroupAsync(int workspaceId, int groupId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)

        {

            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))

                return;



            if (await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))

                return;



            throw new AccessDeniedException("You do not have access to place resources in this group.");

        }



        public async Task<bool> CreateAssetWithContentAsync(
            Asset asset,
            IReadOnlyList<int> groupIds,
            string contentType,
            string? fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(asset);
            ArgumentNullException.ThrowIfNull(content);

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type is required.", nameof(contentType));

            if (!AssetContentFormats.IsZipContent(contentType, fileName))
                throw new ArgumentException("Asset content must be a ZIP file.", nameof(contentType));

            var buffered = await BufferZipContentAsync(content, cancellationToken);

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            EnsureCanCreateAsset(membership);
            var userId = RequireCurrentUserId();

            var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(asset.GroupId, groupIds.ToList());
            await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                _groupRepository,
                _groupAccessResolver,
                workspaceId,
                resolvedGroupIds,
                membership,
                userId,
                cancellationToken);

            asset.WorkspaceId = workspaceId;
            asset.GroupId = ResourceGroupPlacement.PrimaryGroupId(resolvedGroupIds);
            asset.GroupIds = resolvedGroupIds.ToList();
            asset.OwnerUserId = userId;
            asset.Name = RequireTrimmed(asset.Name, nameof(asset.Name));
            asset.Description = string.IsNullOrWhiteSpace(asset.Description) ? null : asset.Description.Trim();
            asset.AssetType = RequireTrimmed(asset.AssetType, nameof(asset.AssetType));
            asset.StorageUrl = string.Empty;
            asset.CreatedAtUtc = DateTime.UtcNow;
            asset.UpdatedAtUtc = asset.CreatedAtUtc;

            var id = await _assetRepository.CreateAssetAsync(workspaceId, asset, cancellationToken);
            if (id <= 0)
                return false;

            asset.Id = id;
            asset.StorageUrl = AssetContentUrls.GetRelativeUrl(id);

            try
            {
                await _assetRepository.UpdateAssetStorageUrlAsync(workspaceId, id, asset.StorageUrl, cancellationToken);
                if (resolvedGroupIds.Count > 0)
                {
                    await _assetRepository.ReplaceAssetGroupSharesAsync(workspaceId, id, resolvedGroupIds, cancellationToken);
                    await _assetRepository.SyncAssetPrimaryGroupIdAsync(workspaceId, id, cancellationToken);
                }

                buffered.Position = 0;
                await _assetContentStore.SaveAsync(workspaceId, id, contentType.Trim(), fileName, buffered, cancellationToken);
            }
            catch
            {
                await _assetContentStore.DeleteAsync(workspaceId, id, cancellationToken);
                await _assetRepository.DeleteAssetAsync(workspaceId, id, cancellationToken);
                throw;
            }

            return true;
        }



        public async Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken)

        {

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);

            var userId = RequireCurrentUserId();

            var assets = await _assetRepository.GetAllAssetsAsync(workspaceId, cancellationToken);

            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, assets, cancellationToken);

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);

            return assets

                .Where(asset => WorkspaceContentVisibility.IsAssetVisibleToUser(asset, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds))

                .ToList();

        }



        public async Task<List<Asset>> GetAssetsInGroupAsync(int groupId, CancellationToken cancellationToken)

        {

            if (groupId <= 0)

                throw new ArgumentOutOfRangeException(nameof(groupId));



            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);

            var userId = RequireCurrentUserId();



            if (!WorkspaceRolePermissions.CanSeeAllContent(membership.Role)

                && !await _groupAccessResolver.HasEffectiveAccessAsync(workspaceId, userId, groupId, cancellationToken))

            {

                throw new AccessDeniedException("You do not have access to this group.");

            }



            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);

            var assets = await _assetRepository.GetAssetsByGroupAsync(workspaceId, groupId, cancellationToken);
            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, assets, cancellationToken);
            return assets;

        }



        public async Task<List<Asset>> GetMyAssetsAsync(CancellationToken cancellationToken)

        {

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);

            var userId = RequireCurrentUserId();

            var assets = await _assetRepository.GetAssetsByOwnerAsync(workspaceId, userId, cancellationToken);
            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, assets, cancellationToken);
            return assets;

        }



        public async Task<Asset?> GetAssetByIdAsync(int assetId, CancellationToken cancellationToken)

        {

            if (assetId <= 0)

                throw new ArgumentOutOfRangeException(nameof(assetId));



            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);

            var asset = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);

            if (asset is null)

                return null;



            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, asset, cancellationToken);

            var userId = RequireCurrentUserId();

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);

            return WorkspaceContentVisibility.IsAssetVisibleToUser(asset, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds)

                ? asset

                : null;

        }



        public async Task<Asset?> UpdateAssetAsync(Asset asset, IReadOnlyList<int>? groupIds, CancellationToken cancellationToken)

        {

            ArgumentNullException.ThrowIfNull(asset);

            if (asset.Id <= 0)

                throw new ArgumentOutOfRangeException(nameof(asset.Id));



            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);

            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, asset.Id, cancellationToken);

            if (existing is null)

                return null;



            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, existing, cancellationToken);
            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);

            if (groupIds is not null)
            {
                var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(asset.GroupId, groupIds.ToList());
                await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                    _groupRepository,
                    _groupAccessResolver,
                    workspaceId,
                    resolvedGroupIds,
                    membership,
                    RequireCurrentUserId(),
                    cancellationToken);
                await _assetRepository.ReplaceAssetGroupSharesAsync(workspaceId, asset.Id, resolvedGroupIds, cancellationToken);
                await _assetRepository.SyncAssetPrimaryGroupIdAsync(workspaceId, asset.Id, cancellationToken);
            }



            existing.Name = RequireTrimmed(asset.Name, nameof(asset.Name));

            existing.Description = string.IsNullOrWhiteSpace(asset.Description) ? null : asset.Description.Trim();

            existing.AssetType = RequireTrimmed(asset.AssetType, nameof(asset.AssetType));



            var updated = await _assetRepository.UpdateAssetAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, updated, cancellationToken);
            return updated;

        }



        public async Task<AssetContent?> GetAssetContentAsync(int assetId, CancellationToken cancellationToken)

        {

            if (assetId <= 0)

                throw new ArgumentOutOfRangeException(nameof(assetId));



            var asset = await GetAssetByIdAsync(assetId, cancellationToken);

            if (asset is null)

                return null;



            return await _assetContentStore.GetAsync(asset.WorkspaceId, assetId, cancellationToken);

        }



        public async Task SaveAssetContentAsync(int assetId, string contentType, string? fileName, Stream content, CancellationToken cancellationToken)

        {

            ArgumentNullException.ThrowIfNull(content);



            if (string.IsNullOrWhiteSpace(contentType))

                throw new ArgumentException("Content type is required.", nameof(contentType));



            if (!AssetContentFormats.IsZipContent(contentType, fileName))

                throw new ArgumentException("Asset content must be a ZIP file.", nameof(contentType));



            if (content.CanSeek && content.Length > _maxAssetBytes)

                throw new ArgumentException($"Asset content must be {_maxAssetBytes / (1024 * 1024)} MB or smaller.");



            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);

            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken)

                ?? throw new KeyNotFoundException("Asset not found.");



            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);



            var buffered = await BufferZipContentAsync(content, cancellationToken);
            buffered.Position = 0;
            await _assetContentStore.SaveAsync(workspaceId, assetId, contentType.Trim(), fileName, buffered, cancellationToken);
        }

        private async Task<MemoryStream> BufferZipContentAsync(Stream content, CancellationToken cancellationToken)
        {
            if (content.CanSeek && content.Length > _maxAssetBytes)
                throw new ArgumentException($"Asset content must be {_maxAssetBytes / (1024 * 1024)} MB or smaller.");

            var buffered = new MemoryStream();
            await content.CopyToAsync(buffered, cancellationToken);

            if (buffered.Length > _maxAssetBytes)
                throw new ArgumentException($"Asset content must be {_maxAssetBytes / (1024 * 1024)} MB or smaller.");

            return buffered;
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

            await _assetContentStore.DeleteAsync(workspaceId, assetId, cancellationToken);

            return await _assetRepository.DeleteAssetAsync(workspaceId, assetId, cancellationToken);

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



        public async Task<bool> CanCurrentUserViewAssetDirectlyAsync(int assetId, CancellationToken cancellationToken)

        {

            var asset = await GetAssetByIdAsync(assetId, cancellationToken);

            return asset is not null;

        }

        public async Task<List<int>> GetAssetGroupIdsAsync(int assetId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var asset = await GetAssetByIdAsync(assetId, cancellationToken);
            if (asset is null)
                throw new KeyNotFoundException("Asset not found.");

            return asset.GroupIds;
        }

        public async Task<List<int>?> SetAssetGroupIdsAsync(int assetId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return null;

            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, existing, cancellationToken);
            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);

            var resolvedGroupIds = ResourceGroupPlacement.ResolveGroupIds(0, groupIds.ToList());
            await ContentGroupShareOperations.EnsureCanPlaceInGroupsAsync(
                _groupRepository,
                _groupAccessResolver,
                workspaceId,
                resolvedGroupIds,
                membership,
                RequireCurrentUserId(),
                cancellationToken);

            await _assetRepository.ReplaceAssetGroupSharesAsync(workspaceId, assetId, resolvedGroupIds, cancellationToken);
            await _assetRepository.SyncAssetPrimaryGroupIdAsync(workspaceId, assetId, cancellationToken);
            return resolvedGroupIds.ToList();
        }

        public async Task<bool> AddAssetGroupAsync(int assetId, int groupId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return false;

            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, existing, cancellationToken);
            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, groupId, membership, RequireCurrentUserId(), cancellationToken);

            var added = await _assetRepository.AddAssetGroupShareAsync(workspaceId, assetId, groupId, cancellationToken);
            if (!added && !existing.GroupIds.Contains(groupId))
                return false;

            await _assetRepository.SyncAssetPrimaryGroupIdAsync(workspaceId, assetId, cancellationToken);
            return true;
        }

        public async Task<bool> RemoveAssetGroupAsync(int assetId, int groupId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return false;

            await ContentGroupShareOperations.PopulateAssetGroupIdsAsync(_assetRepository, workspaceId, existing, cancellationToken);
            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);

            var removed = await _assetRepository.RemoveAssetGroupShareAsync(workspaceId, assetId, groupId, cancellationToken);
            if (!removed)
                return false;

            await _assetRepository.SyncAssetPrimaryGroupIdAsync(workspaceId, assetId, cancellationToken);
            return true;
        }

    }

}


