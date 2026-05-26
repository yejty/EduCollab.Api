using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetFolderRepository _assetFolderRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ICurrentUser _currentUser;

        public AssetService(
            IAssetRepository assetRepository,
            IAssetFolderRepository assetFolderRepository,
            IWorkspaceRepository workspaceRepository,
            ICurrentUser currentUser)
        {
            _assetRepository = assetRepository;
            _assetFolderRepository = assetFolderRepository;
            _workspaceRepository = workspaceRepository;
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

        private async Task<WorkspaceMember> RequireWorkspaceMembershipAsync(int workspaceId, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            var userId = RequireCurrentUserId();

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                throw new KeyNotFoundException("Workspace not found.");

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
            if (membership is null)
                throw new AccessDeniedException("You are not a member of this workspace.");

            return membership;
        }

        private static bool CanManageByWorkspaceRole(WorkspaceMember membership)
        {
            return membership.Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        }

        private async Task EnsureFolderBelongsToWorkspaceAsync(int workspaceId, int? folderId, CancellationToken cancellationToken)
        {
            if (folderId is null)
                return;

            var folder = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId.Value, cancellationToken);
            if (folder is null)
                throw new KeyNotFoundException("Asset folder not found.");
        }

        private async Task EnsureCanManageAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            var membership = await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            var userId = RequireCurrentUserId();

            if (asset.OwnerUserId == userId)
                return;

            if (!CanManageByWorkspaceRole(membership))
                throw new AccessDeniedException("Only the asset owner or workspace owners/admins can manage this asset.");
        }

        public async Task<bool> CreateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            ArgumentNullException.ThrowIfNull(asset);

            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            var userId = RequireCurrentUserId();

            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, asset.FolderId, cancellationToken);

            asset.WorkspaceId = workspaceId;
            asset.OwnerUserId = userId;
            asset.Name = RequireTrimmed(asset.Name, nameof(asset.Name));
            asset.AssetType = RequireTrimmed(asset.AssetType, nameof(asset.AssetType));
            asset.StorageProvider = RequireTrimmed(asset.StorageProvider, nameof(asset.StorageProvider));
            asset.StorageKey = RequireTrimmed(asset.StorageKey, nameof(asset.StorageKey));
            asset.MimeType = string.IsNullOrWhiteSpace(asset.MimeType) ? null : asset.MimeType.Trim();
            asset.Description = string.IsNullOrWhiteSpace(asset.Description) ? null : asset.Description.Trim();
            asset.CreatedAtUtc = DateTime.UtcNow;
            asset.UpdatedAtUtc = asset.CreatedAtUtc;

            var id = await _assetRepository.CreateAssetAsync(workspaceId, asset, cancellationToken);
            if (id <= 0)
                return false;

            asset.Id = id;
            return true;
        }

        public async Task<List<Asset>> GetAllAssetsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            return await _assetRepository.GetAllAssetsAsync(workspaceId, cancellationToken);
        }

        public async Task<List<Asset>> GetAssetsInFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, folderId, cancellationToken);
            return await _assetRepository.GetAssetsByFolderAsync(workspaceId, folderId, cancellationToken);
        }

        public async Task<List<Asset>> GetMyAssetsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            var userId = RequireCurrentUserId();
            return await _assetRepository.GetAssetsByOwnerAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<Asset?> GetAssetByIdAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            return await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
        }

        public async Task<Asset?> UpdateAssetAsync(int workspaceId, Asset asset, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            ArgumentNullException.ThrowIfNull(asset);
            if (asset.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(asset.Id));

            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, asset.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, asset.FolderId, cancellationToken);

            existing.Name = RequireTrimmed(asset.Name, nameof(asset.Name));
            existing.Description = string.IsNullOrWhiteSpace(asset.Description) ? null : asset.Description.Trim();
            existing.AssetType = RequireTrimmed(asset.AssetType, nameof(asset.AssetType));
            existing.FolderId = asset.FolderId;
            existing.StorageProvider = RequireTrimmed(asset.StorageProvider, nameof(asset.StorageProvider));
            existing.StorageKey = RequireTrimmed(asset.StorageKey, nameof(asset.StorageKey));
            existing.MimeType = string.IsNullOrWhiteSpace(asset.MimeType) ? null : asset.MimeType.Trim();
            existing.SizeInBytes = asset.SizeInBytes;

            return await _assetRepository.UpdateAssetAsync(workspaceId, existing, cancellationToken);
        }

        public async Task<Asset?> MoveAssetAsync(int workspaceId, int assetId, int? folderId, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, folderId, cancellationToken);

            return await _assetRepository.MoveAssetAsync(workspaceId, assetId, folderId, cancellationToken);
        }

        public async Task<bool> DeleteAssetAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));
            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var existing = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageAssetAsync(workspaceId, existing, cancellationToken);
            return await _assetRepository.DeleteAssetAsync(workspaceId, assetId, cancellationToken);
        }

        public async Task<bool> CanCurrentUserManageAssetAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken)
        {
            try
            {
                var membership = await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
                var userId = RequireCurrentUserId();
                return userId == ownerUserId || CanManageByWorkspaceRole(membership);
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
    }
}
