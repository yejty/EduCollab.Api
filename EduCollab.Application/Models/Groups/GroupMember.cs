namespace EduCollab.Application.Models.Groups
{
    public class GroupMember
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public GroupRole Role { get; set; }
        public DateTime JoinedAtUtc { get; set; }
    }
}
