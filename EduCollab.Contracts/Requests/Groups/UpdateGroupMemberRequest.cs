using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Groups
{
    public class UpdateGroupMemberRequest
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
