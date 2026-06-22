namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupMembersResponse : PagedCollectionResponse
    {
        public List<GroupMemberResponse> Members { get; set; } = new();
    }
}
