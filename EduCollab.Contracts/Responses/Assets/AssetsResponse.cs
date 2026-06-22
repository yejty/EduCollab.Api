namespace EduCollab.Contracts.Responses.Assets
{
    public class AssetsResponse : PagedCollectionResponse
    {
        public List<AssetResponse> Assets { get; set; } = new();
    }
}
