namespace EduCollab.Contracts.Responses.Scenes
{
    public class SceneAssetsResponse
    {
        public List<SceneAssetResponse> Assets { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
