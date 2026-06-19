namespace EduCollab.Application.Models
{
    public class WorkspaceCreationRequest
    {
        public long Id { get; set; }

        public int RequestedByUserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public WorkspaceCreationRequestStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ReviewedAtUtc { get; set; }

        public int? ReviewedByUserId { get; set; }

        public string? DenialReason { get; set; }
    }
}
