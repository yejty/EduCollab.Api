namespace EduCollab.Contracts.Requests.Flows
{
    public class CreateFlowSceneRequest
    {
        public int FlowId { get; set; }
        public int SceneId { get; set; }
        public int SortOrder { get; set; }
    }
}
