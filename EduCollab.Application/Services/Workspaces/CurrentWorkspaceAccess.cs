using EduCollab.Application.Exceptions;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Workspaces
{
    internal static class CurrentWorkspaceAccess
    {
        public static async Task<int?> ResolveActiveWorkspaceIdAsync(
            IUserRepository userRepository,
            IWorkspaceRepository workspaceRepository,
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await userRepository.GetUserByIdAsync(userId, cancellationToken);
            if (user is null)
                return null;

            if (user.WorkspaceId is int preferred && preferred > 0
                && await workspaceRepository.IsUserWorkspaceMemberAsync(preferred, userId, cancellationToken))
            {
                return preferred;
            }

            var memberships = await workspaceRepository.GetWorkspaceMembershipsForUserAsync(userId, cancellationToken);
            return memberships.FirstOrDefault()?.WorkspaceId;
        }

        public static async Task<(int WorkspaceId, WorkspaceMember Membership)> RequireMembershipAsync(
            IUserRepository userRepository,
            IWorkspaceRepository workspaceRepository,
            int userId,
            CancellationToken cancellationToken)
        {
            var workspaceId = await ResolveActiveWorkspaceIdAsync(userRepository, workspaceRepository, userId, cancellationToken);
            if (workspaceId is null)
                throw new AccessDeniedException("You are not a member of any workspace.");

            var membership = await workspaceRepository.GetWorkspaceMemberAsync(workspaceId.Value, userId, cancellationToken);
            if (membership is null)
                throw new AccessDeniedException("You are not a member of this workspace.");

            return (workspaceId.Value, membership);
        }
    }
}
