using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Assets
{
    public class AssetFolderService : IAssetFolderService
    {
        private readonly IAssetFolderRepository _assetFolderRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ICurrentUser _currentUser;

        public AssetFolderService(
            IAssetFolderRepository assetFolderRepository,
            IWorkspaceRepository workspaceRepository,
            ICurrentUser currentUser)
        {
            _assetFolderRepository = assetFolderRepository;
            _workspaceRepository = workspaceRepository;
            _currentUser = currentUser;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private static string ValidateAndNormalizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name is required.", nameof(name));

            var normalized = name.Trim();
            if (normalized.Contains('/'))
                throw new ArgumentException("Folder name cannot contain '/'.", nameof(name));

            return normalized;
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

        private static bool CanManageWorkspaceAssets(WorkspaceMember membership)
        {
            return membership.Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        }

        private async Task EnsureCanManageWorkspaceAssetsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            var membership = await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            if (!CanManageWorkspaceAssets(membership))
                throw new AccessDeniedException("Only workspace owners and admins can manage asset folders.");
        }

        private async Task<(AssetFolder? Parent, string Path)> ResolvePathAsync(int workspaceId, int? parentFolderId, string name, CancellationToken cancellationToken)
        {
            if (parentFolderId is null)
            {
                return (null, "/" + name);
            }

            var parent = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, parentFolderId.Value, cancellationToken);
            if (parent is null)
                throw new KeyNotFoundException("Parent folder was not found.");

            return (parent, parent.Path.TrimEnd('/') + "/" + name);
        }

        public async Task<bool> CreateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            ArgumentNullException.ThrowIfNull(folder);

            await EnsureCanManageWorkspaceAssetsAsync(workspaceId, cancellationToken);

            var normalizedName = ValidateAndNormalizeFolderName(folder.Name);
            var (_, path) = await ResolvePathAsync(workspaceId, folder.ParentFolderId, normalizedName, cancellationToken);

            folder.WorkspaceId = workspaceId;
            folder.Name = normalizedName;
            folder.Path = path;
            folder.CreatedByUserId = RequireCurrentUserId();
            folder.CreatedAtUtc = DateTime.UtcNow;
            folder.UpdatedAtUtc = folder.CreatedAtUtc;

            var id = await _assetFolderRepository.CreateAssetFolderAsync(workspaceId, folder, cancellationToken);
            if (id <= 0)
                return false;

            folder.Id = id;
            return true;
        }

        public async Task<List<AssetFolder>> GetRootAssetFoldersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            return await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, null, cancellationToken);
        }

        public async Task<List<AssetFolder>> GetAllAssetFoldersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);

            var folders = await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, null, cancellationToken);
            var result = new List<AssetFolder>(folders);
            var queue = new Queue<AssetFolder>(folders);

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

            return result
                .OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Id)
                .ToList();
        }

        public async Task<List<AssetFolder>> GetSubFoldersAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);

            var folder = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (folder is null)
                throw new KeyNotFoundException("Asset folder not found.");

            return await _assetFolderRepository.GetAssetFoldersAsync(workspaceId, folderId, cancellationToken);
        }

        public async Task<AssetFolder?> GetAssetFolderByIdAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
            return await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
        }

        public async Task<AssetFolder?> UpdateAssetFolderAsync(int workspaceId, AssetFolder folder, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            ArgumentNullException.ThrowIfNull(folder);
            if (folder.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(folder.Id));

            await EnsureCanManageWorkspaceAssetsAsync(workspaceId, cancellationToken);

            var existing = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folder.Id, cancellationToken);
            if (existing is null)
                return null;

            var normalizedName = ValidateAndNormalizeFolderName(folder.Name);
            var oldPath = existing.Path;

            if (folder.ParentFolderId == folder.Id)
                throw new ArgumentException("A folder cannot be its own parent.");

            var (parent, newPath) = await ResolvePathAsync(workspaceId, folder.ParentFolderId, normalizedName, cancellationToken);
            if (parent is not null && parent.Path.StartsWith(oldPath + "/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A folder cannot be moved under one of its descendants.");

            existing.ParentFolderId = folder.ParentFolderId;
            existing.Name = normalizedName;
            existing.Path = newPath;

            var updated = await _assetFolderRepository.UpdateAssetFolderAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            if (!string.Equals(oldPath, updated.Path, StringComparison.Ordinal))
            {
                await _assetFolderRepository.UpdateDescendantPathsAsync(workspaceId, oldPath, updated.Path, cancellationToken);
            }

            return updated;
        }

        public async Task<bool> DeleteAssetFolderAsync(int workspaceId, int folderId, CancellationToken cancellationToken)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));
            if (folderId <= 0)
                throw new ArgumentOutOfRangeException(nameof(folderId));

            await EnsureCanManageWorkspaceAssetsAsync(workspaceId, cancellationToken);

            var existing = await _assetFolderRepository.GetAssetFolderByIdAsync(workspaceId, folderId, cancellationToken);
            if (existing is null)
                return false;

            return await _assetFolderRepository.DeleteAssetFolderAsync(workspaceId, folderId, cancellationToken);
        }

        public async Task<bool> CanCurrentUserManageWorkspaceAssetsAsync(int workspaceId, CancellationToken cancellationToken)
        {
            try
            {
                var membership = await RequireWorkspaceMembershipAsync(workspaceId, cancellationToken);
                return CanManageWorkspaceAssets(membership);
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
