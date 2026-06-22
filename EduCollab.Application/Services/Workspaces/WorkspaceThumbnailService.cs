using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Workspaces
{
    public sealed class WorkspaceThumbnailService : IWorkspaceThumbnailService
    {
        private const int MaxThumbnailBytes = 2 * 1024 * 1024;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
        };

        private readonly IWorkspaceThumbnailStore _store;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public WorkspaceThumbnailService(
            IWorkspaceThumbnailStore store,
            IWorkspaceRepository workspaceRepository,
            IUserRepository userRepository,
            ICurrentUser currentUser)
        {
            _store = store;
            _workspaceRepository = workspaceRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<WorkspaceThumbnailContent?> GetCurrentWorkspaceThumbnailAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, _) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            return await _store.GetAsync(workspaceId, cancellationToken);
        }

        public async Task SaveCurrentWorkspaceThumbnailAsync(string contentType, Stream content, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType.Trim()))
            {
                throw new ArgumentException("Thumbnail must be a JPEG, PNG, WebP, or GIF image.");
            }

            if (content.CanSeek && content.Length > MaxThumbnailBytes)
            {
                throw new ArgumentException($"Thumbnail must be {MaxThumbnailBytes / (1024 * 1024)} MB or smaller.");
            }

            var (workspaceId, membership) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanManageWorkspace(membership.Role))
            {
                throw new AccessDeniedException("Only the workspace owner can update the workspace thumbnail.");
            }

            await using var buffered = new MemoryStream();
            await content.CopyToAsync(buffered, cancellationToken);
            if (buffered.Length > MaxThumbnailBytes)
            {
                throw new ArgumentException($"Thumbnail must be {MaxThumbnailBytes / (1024 * 1024)} MB or smaller.");
            }

            buffered.Position = 0;
            await _store.SaveAsync(workspaceId, contentType.Trim(), buffered, cancellationToken);
        }

        public async Task DeleteCurrentWorkspaceThumbnailAsync(CancellationToken cancellationToken)
        {
            var (workspaceId, membership) = await RequireCurrentWorkspaceMembershipAsync(cancellationToken);
            if (!WorkspaceRolePermissions.CanManageWorkspace(membership.Role))
            {
                throw new AccessDeniedException("Only the workspace owner can delete the workspace thumbnail.");
            }

            await _store.DeleteAsync(workspaceId, cancellationToken);
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        private Task<(int WorkspaceId, WorkspaceMember Membership)> RequireCurrentWorkspaceMembershipAsync(CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUserId();
            return CurrentWorkspaceAccess.RequireMembershipAsync(
                _userRepository,
                _workspaceRepository,
                userId,
                cancellationToken);
        }
    }
}
