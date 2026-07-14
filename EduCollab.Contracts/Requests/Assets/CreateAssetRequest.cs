namespace EduCollab.Contracts.Requests.Assets
{
    public class CreateAssetRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssetType { get; set; } = "Package";

        /// <summary>
        /// Groups that receive access to this asset when it is created.
        /// Omit to keep the asset in your personal space.
        /// </summary>
        public List<int>? GroupIds { get; set; }
    }
}
