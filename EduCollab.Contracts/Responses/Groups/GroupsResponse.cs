namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupsResponse : PagedCollectionResponse
    {
        public List<GroupResponse> Groups { get; set; } = new();
    }
}
