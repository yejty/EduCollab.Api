namespace EduCollab.Application.Models
{
    public class Flow
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int OwnerUserId { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
