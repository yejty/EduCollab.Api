namespace EduCollab.Application.Models
{
    public sealed class FlowSceneContextItem
    {
        public int SceneId { get; set; }

        public int FlowId { get; set; }

        public int WorkspaceId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool UsableInFlow { get; set; }

        public bool CanViewDirectly { get; set; }

        public bool IncludeAssets { get; set; }

        public string? DownloadToken { get; set; }

        public string? CacheKey { get; set; }

        public string? Message { get; set; }
    }

    public sealed class FlowScenesManifest
    {
        public int FlowId { get; set; }

        public bool IncludeAssets { get; set; }

        public string? Message { get; set; }

        public List<FlowSceneContextItem> Scenes { get; set; } = new();
    }

    public sealed class FlowSceneContent
    {
        public int SceneId { get; set; }

        public string ContentType { get; set; } = "application/json";

        public string FileName { get; set; } = "scene.json";

        public byte[] Data { get; set; } = [];
    }
}
