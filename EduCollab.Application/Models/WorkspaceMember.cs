namespace EduCollab.Application.Models
{
    public class WorkspaceMember
    {
        public int UserId { get; set; }
        public int WorkspaceId { get; set; }
        public string Email { get; set; } = string.Empty;
        public WorkspaceRole Role { get; set; }
        public DateTime JoinedAtUtc { get; set; }
        public IReadOnlySet<string> Parameters { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
