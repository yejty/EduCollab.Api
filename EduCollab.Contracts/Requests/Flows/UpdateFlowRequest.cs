namespace EduCollab.Contracts.Requests.Flows
{
    public class UpdateFlowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int>? GroupIds { get; set; }
        public List<int>? SceneIds { get; set; }
    }
}
