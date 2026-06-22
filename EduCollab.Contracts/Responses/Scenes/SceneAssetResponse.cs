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
        public string ResolvedFrom { get; set; } = string.Empty;
    }
}
