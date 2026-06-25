namespace EduCollab.Contracts.Responses.Assets
{
    public class AssetResponse
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int GroupId { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CanManage { get; set; }
    }
}
