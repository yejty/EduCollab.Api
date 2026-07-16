using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Users
{
    public class UpdateUserProfileRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
