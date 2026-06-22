using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Assets;
using EduCollab.Application.Services.Content;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Scenes
{
    public class SceneService : ISceneService
    {
        private const string EmptySceneJson = "{}";

        private readonly ISceneRepository _sceneRepository;
        private readonly ISceneContentStore _sceneContentStore;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetService _assetService;
        private readonly IGroupRepository _groupRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;
        private readonly long _maxSceneJsonBytes;

        public SceneService(
            ISceneRepository sceneRepository,
            ISceneContentStore sceneContentStore,
            IAssetRepository assetRepository,
            IAssetService assetService,
            IGroupRepository groupRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser,
            IOptions<WorkspaceContentStorageOptions> contentStorageOptions)
        {
            _sceneRepository = sceneRepository;
            _sceneContentStore = sceneContentStore;
            _assetRepository = assetRepository;
            _assetService = assetService;
            _groupRepository = groupRepository;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _maxSceneJsonBytes = contentStorageOptions.Value.MaxSceneJsonBytes;
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

        private static string CreateETag()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static void EnsureJsonSize(string jsonContent, long maxBytes)
        {
            if (System.Text.Encoding.UTF8.GetByteCount(jsonContent) > maxBytes)
                throw new ArgumentException($"Scene content must be {maxBytes / (1024 * 1024)} MB or smaller.");
        }

        private async Task<(int WorkspaceId, WorkspaceMember Membership)> RequireWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
            if (user?.WorkspaceId is not int workspaceId || workspaceId <= 0)
                throw new AccessDeniedException("You must belong to a workspace to access scenes.");

            var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                throw new KeyNotFoundException("Workspace not found.");

            var membership = await _workspaceRepository.GetWorkspaceMemberAsync(workspaceId, userId, cancellationToken);
            if (membership is null)
                throw new AccessDeniedException("You are not a member of this workspace.");

            return (workspaceId, membership);
        }

        private async Task EnsureGroupBelongsToWorkspaceAsync(int workspaceId, int groupId, CancellationToken cancellationToken)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));

            var group = await _groupRepository.GetGroupByIdAsync(workspaceId, groupId, cancellationToken);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");
        }

        private async Task EnsureCanShareWithGroupOnCreateAsync(int workspaceId, int groupId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanShareContent(membership.Role))
                return;

            var groupMember = await _groupRepository.GetGroupMemberAsync(workspaceId, groupId, userId, cancellationToken);
            if (groupMember is null)
                throw new AccessDeniedException("You must belong to the selected group to share with it.");
        }

        private sealed record SceneVisibilityContext(
            bool CanSeeAllContent,
            int UserId,
            HashSet<int> DirectlySharedSceneIds);

        private async Task<SceneVisibilityContext> BuildSceneVisibilityContextAsync(
            int workspaceId,
            WorkspaceMember membership,
            int userId,
            CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return new SceneVisibilityContext(true, userId, []);

            var userGroupIds = (await _groupRepository.GetUserGroupIdsAsync(workspaceId, userId, cancellationToken)).ToHashSet();
            var sceneShares = await _sceneRepository.GetWorkspaceSceneSharesAsync(workspaceId, cancellationToken);
            var directlySharedSceneIds = sceneShares
                .Where(share => userGroupIds.Contains(share.GroupId))
                .Select(share => share.SceneId)
                .ToHashSet();

            return new SceneVisibilityContext(false, userId, directlySharedSceneIds);
        }

        private static bool IsSceneVisible(Scene scene, SceneVisibilityContext context) =>
            WorkspaceContentVisibility.IsSceneVisibleToUser(
                scene,
                context.UserId,
                context.CanSeeAllContent,
                context.DirectlySharedSceneIds);

        private static bool CanManageScene(WorkspaceMember membership, int ownerUserId, int userId)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return true;

            if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
                return false;

            if (membership.Role == WorkspaceRole.Manager)
                return ownerUserId == userId;

            if (membership.Role == WorkspaceRole.Creator)
                return ownerUserId == userId;

            return ownerUserId == userId;
        }

        private async Task EnsureCanManageSceneAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (CanManageScene(membership, ownerUserId, userId))
                return;

            throw new AccessDeniedException("You do not have permission to manage this scene.");
        }

        private static bool CanCreateScene(WorkspaceMember membership)
        {
            return !WorkspaceRolePermissions.IsReadOnly(membership.Role);
        }

        private static SceneVersion CreateSceneVersionSnapshot(Scene scene, int versionNumber, int createdByUserId) =>
            new()
            {
                SceneId = scene.Id,
                VersionNumber = versionNumber,
                Name = scene.Name,
                Description = scene.Description,
                ETag = scene.ETag,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

        private async Task<string?> LoadSceneContentAsync(int workspaceId, int sceneId, int versionNumber, string? legacyJsonContent, CancellationToken cancellationToken)
        {
            var storedContent = await _sceneContentStore.GetAsync(workspaceId, sceneId, versionNumber, cancellationToken);
            if (storedContent is not null)
                return storedContent;

            if (versionNumber != 1 || string.IsNullOrWhiteSpace(legacyJsonContent) || legacyJsonContent == EmptySceneJson)
                return null;

            await _sceneContentStore.SaveAsync(workspaceId, sceneId, versionNumber, legacyJsonContent, cancellationToken);
            return legacyJsonContent;
        }

        public async Task<bool> CreateSceneAsync(Scene scene, int groupId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);

            if (groupId <= 0)
                throw new ArgumentException("GroupId is required.", nameof(groupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (!CanCreateScene(membership))
                throw new AccessDeniedException("Viewers have read-only access to scenes.");

            var userId = RequireCurrentUserId();

            scene.WorkspaceId = workspaceId;
            scene.OwnerUserId = userId;
            scene.Name = RequireTrimmed(scene.Name, nameof(scene.Name));
            scene.Description = string.IsNullOrWhiteSpace(scene.Description) ? null : scene.Description.Trim();
            var jsonContent = RequireTrimmed(scene.JsonContent, nameof(scene.JsonContent));
            EnsureJsonSize(jsonContent, _maxSceneJsonBytes);
            scene.JsonContent = EmptySceneJson;
            scene.ETag = CreateETag();
            scene.CurrentVersionNumber = 1;
            scene.CreatedAtUtc = DateTime.UtcNow;
            scene.UpdatedAtUtc = scene.CreatedAtUtc;

            var id = await _sceneRepository.CreateSceneAsync(workspaceId, scene, cancellationToken);
            if (id <= 0)
                return false;

            scene.Id = id;
            await _sceneContentStore.SaveAsync(workspaceId, id, 1, jsonContent, cancellationToken);
            scene.JsonContent = jsonContent;
            await _sceneRepository.CreateSceneVersionAsync(workspaceId, CreateSceneVersionSnapshot(scene, 1, userId), cancellationToken);

            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            await EnsureCanShareWithGroupOnCreateAsync(workspaceId, groupId, membership, userId, cancellationToken);

            var share = new SceneGroupShare
            {
                SceneId = id,
                GroupId = groupId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _sceneRepository.CreateSceneShareAsync(workspaceId, share, cancellationToken);

            return true;
        }

        public async Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var scenes = await _sceneRepository.GetAllScenesAsync(workspaceId, cancellationToken);
            var context = await BuildSceneVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return scenes.Where(scene => IsSceneVisible(scene, context)).ToList();
        }

        public async Task<List<Scene>> GetMyScenesAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            return await _sceneRepository.GetScenesByOwnerAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<Scene?> GetSceneByIdAsync(int sceneId, int? versionNumber, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));
            if (versionNumber is <= 0)
                throw new ArgumentOutOfRangeException(nameof(versionNumber));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            var userId = RequireCurrentUserId();
            var context = await BuildSceneVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            if (!IsSceneVisible(scene, context))
                return null;

            var resolvedVersionNumber = versionNumber ?? scene.CurrentVersionNumber;
            if (versionNumber.HasValue)
            {
                var version = await _sceneRepository.GetSceneVersionAsync(workspaceId, sceneId, resolvedVersionNumber, cancellationToken);
                if (version is null)
                    return null;

                scene.Name = version.Name;
                scene.Description = version.Description;
                scene.ETag = version.ETag;
                scene.CurrentVersionNumber = resolvedVersionNumber;
            }

            scene.JsonContent = await LoadSceneContentAsync(workspaceId, sceneId, resolvedVersionNumber, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
            return scene;
        }

        public async Task<Scene?> UpdateSceneAsync(Scene scene, string ifMatch, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);
            if (scene.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(scene.Id));
            if (string.IsNullOrWhiteSpace(ifMatch))
                throw new ArgumentException("If-Match is required.", nameof(ifMatch));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _sceneRepository.GetSceneByIdAsync(workspaceId, scene.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageSceneAsync(existing.OwnerUserId, cancellationToken);

            var normalizedIfMatch = ifMatch.Trim().Trim('"');
            if (!string.Equals(existing.ETag, normalizedIfMatch, StringComparison.Ordinal))
            {
                throw new PreconditionFailedException(
                    "The scene was modified by another request. Reload the scene and retry with the current ETag.");
            }

            var jsonContent = RequireTrimmed(scene.JsonContent, nameof(scene.JsonContent));
            EnsureJsonSize(jsonContent, _maxSceneJsonBytes);

            var previousVersionNumber = existing.CurrentVersionNumber;
            var newVersionNumber = previousVersionNumber + 1;
            var userId = RequireCurrentUserId();

            existing.Name = RequireTrimmed(scene.Name, nameof(scene.Name));
            existing.Description = string.IsNullOrWhiteSpace(scene.Description) ? null : scene.Description.Trim();
            existing.ETag = CreateETag();
            existing.CurrentVersionNumber = newVersionNumber;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            var updated = await _sceneRepository.UpdateSceneAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            await _sceneContentStore.SaveAsync(workspaceId, scene.Id, newVersionNumber, jsonContent, cancellationToken);
            await _sceneRepository.CreateSceneVersionAsync(workspaceId, CreateSceneVersionSnapshot(updated, newVersionNumber, userId), cancellationToken);
            updated.JsonContent = jsonContent;
            return updated;
        }

        public async Task<List<SceneVersion>> GetSceneVersionsAsync(int sceneId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return [];

            var userId = RequireCurrentUserId();
            var context = await BuildSceneVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            if (!IsSceneVisible(scene, context))
                return [];

            return await _sceneRepository.GetSceneVersionsAsync(workspaceId, sceneId, cancellationToken);
        }

        public async Task<SceneVersion?> GetSceneVersionAsync(int sceneId, int versionNumber, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));
            if (versionNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(versionNumber));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            var userId = RequireCurrentUserId();
            var context = await BuildSceneVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            if (!IsSceneVisible(scene, context))
                return null;

            return await _sceneRepository.GetSceneVersionAsync(workspaceId, sceneId, versionNumber, cancellationToken);
        }

        public async Task<bool> DeleteSceneAsync(int sceneId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageSceneAsync(existing.OwnerUserId, cancellationToken);
            var deleted = await _sceneRepository.DeleteSceneAsync(workspaceId, sceneId, cancellationToken);
            if (deleted)
            {
                await _sceneContentStore.DeleteAllVersionsAsync(workspaceId, sceneId, cancellationToken);
            }

            return deleted;
        }

        public async Task<bool> ShareSceneAsync(int sceneId, int groupId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (existing is null)
                return false;

            var userId = RequireCurrentUserId();
            if (!WorkspaceRolePermissions.CanShareContent(membership.Role) && existing.OwnerUserId != userId)
                throw new AccessDeniedException("Only workspace owners and managers can share scenes with groups.");

            await EnsureCanManageSceneAsync(existing.OwnerUserId, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);

            var share = new SceneGroupShare
            {
                SceneId = sceneId,
                GroupId = groupId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _sceneRepository.CreateSceneShareAsync(workspaceId, share, cancellationToken);
            return created is not null;
        }

        public async Task<bool> RemoveSceneShareAsync(int sceneId, int groupId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (existing is null)
                return false;

            await EnsureCanManageSceneAsync(existing.OwnerUserId, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            return await _sceneRepository.DeleteSceneShareAsync(workspaceId, sceneId, groupId, cancellationToken);
        }

        public async Task<List<int>> GetSceneGroupIdsAsync(int sceneId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var shares = await _sceneRepository.GetSceneSharesAsync(workspaceId, sceneId, cancellationToken);
            return shares.Select(share => share.GroupId).ToList();
        }

        public async Task<bool> CanCurrentUserManageSceneAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            try
            {
                var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
                var userId = RequireCurrentUserId();
                return CanManageScene(membership, ownerUserId, userId);
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

        private async Task<Scene?> GetVisibleSceneAsync(int workspaceId, int sceneId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            var context = await BuildSceneVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return IsSceneVisible(scene, context) ? scene : null;
        }

        public async Task<List<SceneAssetContextItem>> GetSceneAssetsAsync(int sceneId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var scene = await GetVisibleSceneAsync(workspaceId, sceneId, membership, userId, cancellationToken);
            if (scene is null)
                throw new KeyNotFoundException("Scene not found.");

            scene.JsonContent = await LoadSceneContentAsync(workspaceId, sceneId, scene.CurrentVersionNumber, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;

            var attachedAssetIds = (await _sceneRepository.GetSceneAssetLinksAsync(workspaceId, sceneId, cancellationToken))
                .Select(link => link.AssetId)
                .ToHashSet();
            var jsonAssetIds = SceneJsonAssetReferenceParser.ExtractAssetIds(scene.JsonContent);

            var resolvedSources = new Dictionary<int, SceneAssetResolvedFrom>();
            foreach (var assetId in attachedAssetIds)
                resolvedSources[assetId] = SceneAssetResolvedFrom.SceneAttachment;

            foreach (var assetId in jsonAssetIds)
            {
                if (!resolvedSources.ContainsKey(assetId))
                    resolvedSources[assetId] = SceneAssetResolvedFrom.SceneJsonReference;
            }

            var items = new List<SceneAssetContextItem>();
            foreach (var (assetId, resolvedFrom) in resolvedSources)
            {
                var asset = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
                if (asset is null)
                    continue;

                var canViewDirectly = await _assetService.CanCurrentUserViewAssetDirectlyAsync(assetId, cancellationToken);
                items.Add(new SceneAssetContextItem
                {
                    AssetId = asset.Id,
                    SceneId = sceneId,
                    WorkspaceId = workspaceId,
                    Name = asset.Name,
                    AssetType = asset.AssetType,
                    UsableInScene = true,
                    CanViewDirectly = canViewDirectly,
                    ResolvedFrom = resolvedFrom
                });
            }

            return items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.AssetId).ToList();
        }

        public async Task<SceneAssetContextItem?> AttachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            await EnsureCanManageSceneAsync(scene.OwnerUserId, cancellationToken);

            var asset = await _assetRepository.GetAssetByIdAsync(workspaceId, assetId, cancellationToken);
            if (asset is null)
                return null;

            var link = new SceneAssetLink
            {
                SceneId = sceneId,
                AssetId = assetId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _sceneRepository.CreateSceneAssetLinkAsync(workspaceId, link, cancellationToken);
            if (created is null)
            {
                var existingLinks = await _sceneRepository.GetSceneAssetLinksAsync(workspaceId, sceneId, cancellationToken);
                if (!existingLinks.Any(existing => existing.AssetId == assetId))
                    return null;
            }

            var canViewDirectly = await _assetService.CanCurrentUserViewAssetDirectlyAsync(assetId, cancellationToken);
            return new SceneAssetContextItem
            {
                AssetId = asset.Id,
                SceneId = sceneId,
                WorkspaceId = workspaceId,
                Name = asset.Name,
                AssetType = asset.AssetType,
                UsableInScene = true,
                CanViewDirectly = canViewDirectly,
                ResolvedFrom = SceneAssetResolvedFrom.SceneAttachment
            };
        }

        public async Task<bool> DetachSceneAssetAsync(int sceneId, int assetId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            if (assetId <= 0)
                throw new ArgumentOutOfRangeException(nameof(assetId));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return false;

            await EnsureCanManageSceneAsync(scene.OwnerUserId, cancellationToken);
            return await _sceneRepository.DeleteSceneAssetLinkAsync(workspaceId, sceneId, assetId, cancellationToken);
        }
    }
}
