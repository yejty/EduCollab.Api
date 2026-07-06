namespace EduCollab.Application.Models
{
    public sealed class WorkspaceInvitationDetails
    {
        public long InvitationId { get; init; }

        public int WorkspaceId { get; init; }

        public string Email { get; init; } = string.Empty;

        public WorkspaceRole Role { get; init; }

        public IReadOnlySet<string> Presets { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
