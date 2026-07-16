namespace EduCollab.Contracts.Responses.Groups
{
    public class ResourceGroupsResponse
    {
        public List<int> GroupIds { get; set; } = new();

        public List<ResourceGroupShareResponse> Groups { get; set; } = new();
    }
}
