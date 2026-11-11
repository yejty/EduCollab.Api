using System.ComponentModel.DataAnnotations;

namespace Space.Contracts.Requests.Groups
{
    public class CreateGroupMemberRequest
    {
        [Required]
        public int UserId { get; set; }
    }
}
