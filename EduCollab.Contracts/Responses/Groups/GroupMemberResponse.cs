namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupMemberResponse
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAtUtc { get; set; }
    }
}
