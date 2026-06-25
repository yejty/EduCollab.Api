using EduCollab.Contracts.Responses;

namespace EduCollab.Contracts.Responses.Flows
{
    public class FlowsResponse : PagedCollectionResponse
    {
        public List<FlowResponse> Flows { get; set; } = new();
    }
}
