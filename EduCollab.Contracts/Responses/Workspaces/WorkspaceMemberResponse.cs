using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Contracts.Responses.Workspaces
{
    /// <summary>
    /// A workspace user projection: user identity plus workspace-scoped role, groups, and join metadata.
    /// </summary>
    public sealed class WorkspaceMemberResponse
    {
        /// <summary>User profile fields; includes <see cref="UserResponse.WorkspaceId"/> when relevant to your API.</summary>
        public UserResponse User { get; set; } = new();

        /// <summary>Workspace-scoped role (e.g. Owner, Admin, Member).</summary>
        public string Role { get; set; } = string.Empty;

        public DateTimeOffset? JoinedAt { get; set; }
    }
}
