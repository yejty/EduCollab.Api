using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;

namespace EduCollab.Application.Services.Scenes
{
    public class SceneService : ISceneService
    {
        private readonly ISceneRepository _sceneRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public SceneService(
            ISceneRepository sceneRepository,
            IGroupRepository groupRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _sceneRepository = sceneRepository;
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

        private static string CreateETag()
        {
            return Guid.NewGuid().ToString("N");
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

        public async Task<bool> CreateSceneAsync(Scene scene, int? groupId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            if (!CanCreateScene(membership))
                throw new AccessDeniedException("Viewers have read-only access to scenes.");

            var userId = RequireCurrentUserId();

            scene.WorkspaceId = workspaceId;
            scene.OwnerUserId = userId;
            scene.Name = RequireTrimmed(scene.Name, nameof(scene.Name));
            scene.Description = string.IsNullOrWhiteSpace(scene.Description) ? null : scene.Description.Trim();
            scene.JsonContent = RequireTrimmed(scene.JsonContent, nameof(scene.JsonContent));
            scene.ETag = CreateETag();
            scene.CreatedAtUtc = DateTime.UtcNow;
            scene.UpdatedAtUtc = scene.CreatedAtUtc;

            var id = await _sceneRepository.CreateSceneAsync(workspaceId, scene, cancellationToken);
            if (id <= 0)
                return false;

            scene.Id = id;

            if (groupId is int selectedGroupId)
            {
                await EnsureGroupBelongsToWorkspaceAsync(workspaceId, selectedGroupId, cancellationToken);
                await EnsureCanShareWithGroupOnCreateAsync(workspaceId, selectedGroupId, membership, userId, cancellationToken);

                var share = new SceneGroupShare
                {
                    SceneId = id,
                    GroupId = selectedGroupId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _sceneRepository.CreateSceneShareAsync(workspaceId, share, cancellationToken);
            }

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

        public async Task<Scene?> GetSceneByIdAsync(int sceneId, CancellationToken cancellationToken)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            var (workspaceId, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var scene = await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
            if (scene is null)
                return null;

            var userId = RequireCurrentUserId();
            var context = await BuildSceneVisibilityContextAsync(workspaceId, membership, userId, cancellationToken);
            return IsSceneVisible(scene, context) ? scene : null;
        }

        public async Task<Scene?> UpdateSceneAsync(Scene scene, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);
            if (scene.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(scene.Id));

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var existing = await _sceneRepository.GetSceneByIdAsync(workspaceId, scene.Id, cancellationToken);
            if (existing is null)
                return null;

            await EnsureCanManageSceneAsync(existing.OwnerUserId, cancellationToken);

            existing.Name = RequireTrimmed(scene.Name, nameof(scene.Name));
            existing.Description = string.IsNullOrWhiteSpace(scene.Description) ? null : scene.Description.Trim();
            existing.JsonContent = RequireTrimmed(scene.JsonContent, nameof(scene.JsonContent));
            existing.ETag = CreateETag();
            existing.UpdatedAtUtc = DateTime.UtcNow;

            return await _sceneRepository.UpdateSceneAsync(workspaceId, existing, cancellationToken);
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
            return await _sceneRepository.DeleteSceneAsync(workspaceId, sceneId, cancellationToken);
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
    }
}
