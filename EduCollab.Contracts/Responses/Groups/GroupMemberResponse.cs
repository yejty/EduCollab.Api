namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupMemberResponse
    {
        public int UserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public List<string> Parameters { get; set; } = new();

        public string Role { get; set; } = string.Empty;

        public DateTime JoinedAt { get; set; }
    }
}
