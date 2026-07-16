namespace EduCollab.Contracts.Requests.Groups
{
    public class SetFlowGroupsRequest
    {
        public List<ResourceGroupShareRequest> Groups { get; set; } = new();
    }
}
