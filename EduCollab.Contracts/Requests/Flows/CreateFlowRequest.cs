namespace EduCollab.Contracts.Requests.Flows
{
    public class CreateFlowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GroupId { get; set; }
    }
}
