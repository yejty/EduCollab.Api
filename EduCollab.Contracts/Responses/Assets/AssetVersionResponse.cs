namespace EduCollab.Contracts.Responses.Assets
{
    public class AssetVersionResponse
    {
        public int AssetId { get; set; }
        public int VersionNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string? VersionLabel { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
