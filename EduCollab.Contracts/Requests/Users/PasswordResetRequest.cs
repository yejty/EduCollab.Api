using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Users
{
    public class PasswordResetRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
