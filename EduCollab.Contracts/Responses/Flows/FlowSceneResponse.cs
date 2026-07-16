namespace EduCollab.Contracts.Responses.Flows
{
    public class FlowSceneResponse
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
}
