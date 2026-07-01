namespace EduCollab.Contracts.Requests.Assets
{
    public class CreateAssetRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssetType { get; set; } = "Package";
        public int GroupId { get; set; }
        public List<int>? GroupIds { get; set; }
    }
}
