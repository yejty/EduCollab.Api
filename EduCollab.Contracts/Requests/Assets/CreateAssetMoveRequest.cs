namespace EduCollab.Contracts.Requests.Assets
{
    public class CreateAssetMoveRequest
    {
        public int AssetId { get; set; }
        public int? FolderId { get; set; }
    }
}
