using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Groups
{
    public class CreateGroupMemberRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
