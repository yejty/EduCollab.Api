using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Users
{
    public class ChangePasswordRequest
    {
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
        
    }
}
