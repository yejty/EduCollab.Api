namespace EduCollab.Application.Models
{
    public sealed class SessionBootstrap
    {
        public int SessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IncludeAssets { get; set; }

        public string? AssetKind { get; set; }

        public string? AssetId { get; set; }

        public string? AssetName { get; set; }

        public string? ColyseusEndpoint { get; set; }

        public List<SessionBootstrapScene> Scenes { get; set; } = [];

        public List<SessionBootstrapAsset> Assets { get; set; } = [];

        public string? Message { get; set; }
    }

    public sealed class SessionBootstrapScene
    {
        public int SceneId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContentUrl { get; set; } = string.Empty;
    }

    public sealed class SessionBootstrapAsset
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

    public sealed class SessionAssetManifest
    {
        public int SessionId { get; set; }

        public int SceneId { get; set; }

        public bool IncludeAssets { get; set; }

        public string? Message { get; set; }

        public List<SessionBootstrapAsset> Assets { get; set; } = [];
    }

    public sealed class SessionJoinTicketResult
    {
        public string JoinTicket { get; set; } = string.Empty;

        public int SessionId { get; set; }

        public string? ColyseusEndpoint { get; set; }

        public string Role { get; set; } = string.Empty;

        public string ColyseusRole { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
    }
}
