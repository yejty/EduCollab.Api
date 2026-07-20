namespace EduCollab.Contracts.Responses.Sessions
{
    public class SessionBootstrapResponse
    {
        public int SessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IncludeAssets { get; set; }

        public string? AssetKind { get; set; }

        public string? AssetId { get; set; }

        public string? AssetName { get; set; }

        public string? ColyseusEndpoint { get; set; }

        public List<SessionBootstrapSceneResponse> Scenes { get; set; } = [];

        public List<SessionBootstrapAssetResponse> Assets { get; set; } = [];

        public string? Message { get; set; }
    }

    public class SessionBootstrapSceneResponse
    {
        public int SceneId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContentUrl { get; set; } = string.Empty;
    }

    public class SessionBootstrapAssetResponse
    {
        public int AssetId { get; set; }

        public int SceneId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public bool DownloadAvailable { get; set; }

        public string? ContentUrl { get; set; }

        public string? CacheKey { get; set; }

        public string? Message { get; set; }
    }

    public class SessionAssetsResponse
    {
        public int SessionId { get; set; }

        public int SceneId { get; set; }

        public bool IncludeAssets { get; set; }

        public string? Message { get; set; }

        public List<SessionBootstrapAssetResponse> Assets { get; set; } = [];
    }
}
