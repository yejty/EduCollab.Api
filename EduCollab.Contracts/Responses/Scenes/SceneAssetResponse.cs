namespace EduCollab.Contracts.Responses.Scenes
{
    public class SceneAssetResponse
    {
        public int AssetId { get; set; }

        public int SceneId { get; set; }

        public int WorkspaceId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public bool UsableInScene { get; set; }

        public bool CanViewDirectly { get; set; }

        public bool IncludeAssets { get; set; }

        public string? DownloadToken { get; set; }

        public string? CacheKey { get; set; }

        public string? Message { get; set; }
    }
}
