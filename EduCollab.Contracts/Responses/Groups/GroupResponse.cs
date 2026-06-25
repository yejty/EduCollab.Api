namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupResponse
    {
        public int Id { get; set; }
        public int? ParentGroupId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Path { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public int UserCount { get; set; }
        public string? CurrentUserRole { get; set; }
    }
}
