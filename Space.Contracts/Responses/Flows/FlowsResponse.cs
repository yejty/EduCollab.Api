using Space.Contracts.Responses;

namespace Space.Contracts.Responses.Flows
{
    public class FlowsResponse : PagedCollectionResponse
    {
        public List<FlowResponse> Flows { get; set; } = new();
    }
}
