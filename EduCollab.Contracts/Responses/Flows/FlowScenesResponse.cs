namespace EduCollab.Contracts.Responses.Flows
{
    public class FlowScenesResponse
    {
        public int FlowId { get; set; }

        public bool IncludeAssets { get; set; }

        public string? Message { get; set; }

        public List<FlowSceneResponse> Scenes { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
    }
}
