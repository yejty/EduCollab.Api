namespace EduCollab.Application.Models
{
    public sealed class SceneAssetContextItem
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

    public sealed class SceneAssetsManifest
    {
        public int SceneId { get; set; }

        public bool IncludeAssets { get; set; }

        public string? Message { get; set; }

        public List<SceneAssetContextItem> Assets { get; set; } = new();
    }
}
