namespace EduCollab.Contracts.Responses.Assets
{
    public class AssetFoldersResponse : PagedCollectionResponse
    {
        public List<AssetFolderResponse> Folders { get; set; } = new();
    }
}
