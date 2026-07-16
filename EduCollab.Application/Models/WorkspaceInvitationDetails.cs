namespace EduCollab.Application.Models
{
    public sealed class WorkspaceInvitationDetails
    {
        public long InvitationId { get; init; }

        public int WorkspaceId { get; init; }

        public string Email { get; init; } = string.Empty;

        /// <summary>
        /// Group the invitee joins on accept. Grants access to this group and all subgroups.
        /// </summary>
        public int GroupId { get; init; }

        public WorkspaceRole Role { get; init; }

        public IReadOnlySet<string> Parameters { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
