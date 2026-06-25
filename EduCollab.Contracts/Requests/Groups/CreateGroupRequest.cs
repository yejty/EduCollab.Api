namespace EduCollab.Contracts.Requests.Groups
{
    public class CreateGroupRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int? ParentGroupId { get; set; }
    }
}
