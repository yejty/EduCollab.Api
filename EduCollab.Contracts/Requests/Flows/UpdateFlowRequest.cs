namespace EduCollab.Contracts.Requests.Flows
{
    public class UpdateFlowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GroupId { get; set; }
        public List<int>? GroupIds { get; set; }
    }
}
