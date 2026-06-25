using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Assets;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Workspaces;
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
        private readonly IGroupAccessResolver _groupAccessResolver;
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
            IGroupAccessResolver groupAccessResolver,
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
            _groupAccessResolver = groupAccessResolver;
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

        private async Task<string?> LoadSceneContentAsync(int workspaceId, int sceneId, string? legacyJsonContent, CancellationToken cancellationToken)
        {
            var storedContent = await _sceneContentStore.GetAsync(workspaceId, sceneId, cancellationToken);
            if (storedContent is not null)
                return storedContent;

            if (string.IsNullOrWhiteSpace(legacyJsonContent) || legacyJsonContent == EmptySceneJson)
                return null;

            await _sceneContentStore.SaveAsync(workspaceId, sceneId, legacyJsonContent, cancellationToken);
            return legacyJsonContent;
        }

        private static void EnsureJsonSize(string jsonContent, long maxBytes)
        {
            if (System.Text.Encoding.UTF8.GetByteCount(jsonContent) > maxBytes)
                throw new ArgumentException($"Scene content must be {maxBytes / (1024 * 1024)} MB or smaller.");
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

        private async Task<HashSet<int>> GetAccessibleGroupIdsAsync(int workspaceId, WorkspaceMember membership, int userId, CancellationToken cancellationToken)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
            {
                var allGroups = await _groupRepository.GetAllGroupsAsync(workspaceId, cancellationToken);
                return allGroups.Select(g => g.Id).ToHashSet();
            }

            return await _groupAccessResolver.GetEffectiveAccessibleGroupIdsAsync(workspaceId, userId, cancellationToken);
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

        private static bool CanManageScene(WorkspaceMember membership, int ownerUserId, int userId)
        {
            if (WorkspaceRolePermissions.CanSeeAllContent(membership.Role))
                return true;

            if (WorkspaceRolePermissions.IsReadOnly(membership.Role))
                return false;

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

        private static bool CanCreateScene(WorkspaceMember membership) =>
            !WorkspaceRolePermissions.IsReadOnly(membership.Role);

        public async Task<bool> CreateSceneAsync(Scene scene, int groupId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);

            if (groupId <= 0)
                throw new ArgumentException("GroupId is required.", nameof(groupId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (!CanCreateScene(membership))
                throw new AccessDeniedException("Viewers have read-only access to scenes.");

            var userId = RequireCurrentUserId();
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, groupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, groupId, membership, userId, cancellationToken);

            scene.WorkspaceId = workspaceId;
            scene.GroupId = groupId;
            scene.OwnerUserId = userId;
            scene.Name = RequireTrimmed(scene.Name, nameof(scene.Name));
            scene.Description = string.IsNullOrWhiteSpace(scene.Description) ? null : scene.Description.Trim();
            var jsonContent = RequireTrimmed(scene.JsonContent, nameof(scene.JsonContent));
            EnsureJsonSize(jsonContent, _maxSceneJsonBytes);
            scene.JsonContent = EmptySceneJson;
            scene.CreatedAtUtc = DateTime.UtcNow;
            scene.UpdatedAtUtc = scene.CreatedAtUtc;

            var id = await _sceneRepository.CreateSceneAsync(workspaceId, scene, cancellationToken);
            if (id <= 0)
                return false;

            scene.Id = id;
            await _sceneContentStore.SaveAsync(workspaceId, id, jsonContent, cancellationToken);
            scene.JsonContent = jsonContent;
            return true;
        }

        public async Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            var scenes = await _sceneRepository.GetAllScenesAsync(workspaceId, cancellationToken);
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return scenes
                .Where(scene => WorkspaceContentVisibility.IsSceneVisibleToUser(scene, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds))
                .ToList();
        }

        public async Task<List<Scene>> GetScenesInGroupAsync(int groupId, CancellationToken cancellationToken)
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
            return await _sceneRepository.GetScenesByGroupAsync(workspaceId, groupId, cancellationToken);
        }

        public async Task<List<Scene>> GetMyScenesAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();
            return await _sceneRepository.GetScenesByOwnerAsync(workspaceId, userId, cancellationToken);
        }

        public async Task<Scene?> GetSceneByIdAsync(int sceneId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            var userId = RequireCurrentUserId();
            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            if (!WorkspaceContentVisibility.IsSceneVisibleToUser(scene, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds))
                return null;

            scene.JsonContent = await LoadSceneContentAsync(workspaceId, sceneId, scene.JsonContent, cancellationToken)
                ?? EmptySceneJson;
            return scene;
        }

        public async Task<Scene?> UpdateSceneAsync(Scene scene, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);
            if (scene.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(scene.Id));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _sceneRepository.GetSceneByIdAsync(workspaceId, scene.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageSceneAsync(existing.OwnerUserId, cancellationToken);
            await EnsureGroupBelongsToWorkspaceAsync(workspaceId, scene.GroupId, cancellationToken);
            await EnsureCanPlaceInGroupAsync(workspaceId, scene.GroupId, membership, RequireCurrentUserId(), cancellationToken);

            var jsonContent = RequireTrimmed(scene.JsonContent, nameof(scene.JsonContent));
            EnsureJsonSize(jsonContent, _maxSceneJsonBytes);

            existing.Name = RequireTrimmed(scene.Name, nameof(scene.Name));
            existing.Description = string.IsNullOrWhiteSpace(scene.Description) ? null : scene.Description.Trim();
            existing.GroupId = scene.GroupId;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            var updated = await _sceneRepository.UpdateSceneAsync(workspaceId, existing, cancellationToken);
            if (updated is null)
                return null;

            await _sceneContentStore.SaveAsync(workspaceId, scene.Id, jsonContent, cancellationToken);
            updated.JsonContent = jsonContent;
            return updated;
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
                await _sceneContentStore.DeleteAsync(workspaceId, sceneId, cancellationToken);

            return deleted;
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

            var accessibleGroupIds = await GetAccessibleGroupIdsAsync(workspaceId, membership, userId, cancellationToken);
            return WorkspaceContentVisibility.IsSceneVisibleToUser(scene, userId, WorkspaceRolePermissions.CanSeeAllContent(membership.Role), accessibleGroupIds)
                ? scene
                : null;
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

            scene.JsonContent = await LoadSceneContentAsync(workspaceId, sceneId, scene.JsonContent, cancellationToken)
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

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
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
