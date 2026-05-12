using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Contracts.Responses.Workspaces
{
    /// <summary>
    /// A user's membership in a workspace: identity (same shape as <see cref="UserResponse"/>) plus role, groups, and join metadata.
    /// </summary>
    public sealed class WorkspaceMemberResponse
    {
        /// <summary>Primary key of the membership row when a WorkspaceMembers (or similar) table exists.</summary>
        public long MembershipId { get; set; }

        /// <summary>User profile fields; includes <see cref="UserResponse.WorkspaceId"/> when relevant to your API.</summary>
        public UserResponse User { get; set; } = new();

        /// <summary>Workspace-scoped role (e.g. Owner, Admin, Member).</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Optional workspace-scoped group labels or ids (strings until a Group entity exists).</summary>
        public List<string> Groups { get; set; } = new();

        public DateTimeOffset? JoinedAt { get; set; }
    }
}
