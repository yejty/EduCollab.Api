namespace EduCollab.Contracts.Requests.Groups
{
    public class SetSceneGroupsRequest
    {
        public List<ResourceGroupShareRequest> Groups { get; set; } = new();
    }
}
