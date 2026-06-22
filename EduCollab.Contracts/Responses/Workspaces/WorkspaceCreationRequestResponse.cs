namespace EduCollab.Contracts.Responses.Workspaces
{
    public class WorkspaceCreationRequestResponse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? DenialReason { get; set; }
    }
}
