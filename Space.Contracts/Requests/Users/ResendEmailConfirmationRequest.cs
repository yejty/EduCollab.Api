using System.ComponentModel.DataAnnotations;

namespace Space.Contracts.Requests.Users
{
    public sealed class ResendEmailConfirmationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
