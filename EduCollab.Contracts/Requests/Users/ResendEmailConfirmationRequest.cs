using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Users
{
    public sealed class ResendEmailConfirmationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
