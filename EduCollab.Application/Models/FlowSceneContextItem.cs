namespace EduCollab.Application.Models
{
    public enum FlowSceneResolvedFrom
    {
        FlowAttachment
    }

    public class FlowSceneContextItem
    {
        public int SceneId { get; set; }
        public int FlowId { get; set; }
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public bool UsableInFlow { get; set; }
        public bool CanViewDirectly { get; set; }
        public FlowSceneResolvedFrom ResolvedFrom { get; set; }
    }
}
