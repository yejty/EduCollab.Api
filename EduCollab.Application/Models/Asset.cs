namespace EduCollab.Application.Models
{
    public class Asset
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int? FolderId { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public string? Version { get; set; }
        public int CurrentVersionNumber { get; set; } = 1;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
