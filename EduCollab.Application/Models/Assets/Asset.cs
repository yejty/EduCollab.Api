namespace EduCollab.Application.Models.Assets
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
        public string StorageProvider { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long? SizeInBytes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
