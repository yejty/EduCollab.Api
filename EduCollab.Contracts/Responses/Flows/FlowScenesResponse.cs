namespace EduCollab.Contracts.Responses.Flows
{
    public class FlowScenesResponse
    {
        public List<FlowSceneResponse> Scenes { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
