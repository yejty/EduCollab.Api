namespace EduCollab.Application.Models.Assets
{
    public class AssetFolder
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int? ParentFolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
