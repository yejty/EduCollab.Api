using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Users
{
    public sealed class ConfirmEmailRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Token { get; set; }
    }
}
