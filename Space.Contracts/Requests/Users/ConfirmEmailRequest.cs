using System.ComponentModel.DataAnnotations;
using Space.Contracts.Validation;

namespace Space.Contracts.Requests.Users
{
    public sealed class ConfirmEmailRequest
    {
        [Required]
        [EmailAddress]
        [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailError)]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }

        [Required]
        public required string Token { get; set; }
    }
}
