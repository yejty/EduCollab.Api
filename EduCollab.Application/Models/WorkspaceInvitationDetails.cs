namespace EduCollab.Application.Models
{
    public sealed class WorkspaceInvitationDetails
    {
        public int WorkspaceId { get; init; }

        public string Email { get; init; } = string.Empty;

        public WorkspaceRole Role { get; init; }
    }
}
