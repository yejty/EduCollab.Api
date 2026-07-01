namespace EduCollab.Contracts.Requests.Groups
{
    public class SetResourceGroupsRequest
    {
        public List<int> GroupIds { get; set; } = new();
    }
}
