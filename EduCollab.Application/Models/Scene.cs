namespace EduCollab.Application.Models
{
    public class Scene
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string JsonContent { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
        public int CurrentVersionNumber { get; set; } = 1;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
