namespace EduCollab.Application.Models
{
    public enum SceneAssetResolvedFrom
    {
        SceneAttachment,
        SceneJsonReference
    }

    public class SceneAssetContextItem
    {
        public int AssetId { get; set; }
        public int SceneId { get; set; }
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public bool UsableInScene { get; set; }
        public bool CanViewDirectly { get; set; }
        public SceneAssetResolvedFrom ResolvedFrom { get; set; }
    }
}
