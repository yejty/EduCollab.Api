namespace EduCollab.Application.Models
{
    public class Group
    {
        public int Id { get; set; }
        public int? ParentGroupId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Path { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public int CreatedByUserId { get; set; }
        public int UserCount { get; set; }
    }
}
