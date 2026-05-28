using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Scenes
{
    public class SceneService : ISceneService
    {
        private readonly ISceneRepository _sceneRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public SceneService(
            ISceneRepository sceneRepository,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _sceneRepository = sceneRepository;
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

        private static bool CanManageByWorkspaceRole(WorkspaceMember membership)
        {
            return membership.Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        }

        private async Task EnsureCanManageSceneAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            var (_, membership) = await RequireWorkspaceMembershipAsync(cancellationToken);
            var userId = RequireCurrentUserId();

            if (ownerUserId == userId)
                return;

            if (!CanManageByWorkspaceRole(membership))
                throw new AccessDeniedException("Only the scene owner or workspace owners/admins can manage this scene.");
        }

        public async Task<bool> CreateSceneAsync(Scene scene, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scene);

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
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
            return true;
        }

        public async Task<List<Scene>> GetAllScenesAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            return await _sceneRepository.GetAllScenesAsync(workspaceId, cancellationToken);
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

            var (workspaceId, _) = await RequireWorkspaceMembershipAsync(cancellationToken);
            return await _sceneRepository.GetSceneByIdAsync(workspaceId, sceneId, cancellationToken);
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

        public async Task<bool> CanCurrentUserManageSceneAsync(int ownerUserId, CancellationToken cancellationToken)
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
    }
}
