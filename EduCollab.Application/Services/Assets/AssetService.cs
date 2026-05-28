using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using System.Linq;

namespace EduCollab.Application.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetFolderRepository _assetFolderRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public AssetService(
            IAssetRepository assetRepository,
            IAssetFolderRepository assetFolderRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _assetRepository = assetRepository;
            _assetFolderRepository = assetFolderRepository;
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
            var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (asset.OwnerUserId == userId)
                return;

            if (!CanManageByWorkspaceRole(membership))
                throw new AccessDeniedException("Only the asset owner or workspace owners/admins can manage this asset.");
        }

        public async Task<bool> CreateAssetAsync(Asset asset, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(asset);

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
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
            return true;
        }

        public async Task<List<Asset>> GetAllAssetsAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            return await _assetRepository.GetAllAssetsAsync(workspaceId, cancellationToken);
        }

        public async Task<List<Asset>> GetAssetsInFolderAsync(int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            await EnsureFolderBelongsToWorkspaceAsync(workspaceId, folderId, cancellationToken);
            return await _assetRepository.GetAssetsByFolderAsync(workspaceId, folderId, cancellationToken);
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

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            return await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
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

        public async Task<bool> CanCurrentUserManageAssetAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            try
            {
                var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
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
