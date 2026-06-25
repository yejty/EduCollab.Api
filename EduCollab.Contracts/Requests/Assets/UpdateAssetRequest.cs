namespace EduCollab.Contracts.Requests.Assets
{
    public class UpdateAssetRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GroupId { get; set; }
        public string AssetType { get; set; } = string.Empty;
    }
}
