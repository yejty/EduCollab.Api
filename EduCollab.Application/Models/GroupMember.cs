namespace EduCollab.Application.Models
{
    public class GroupMember
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAtUtc { get; set; }
    }
}
