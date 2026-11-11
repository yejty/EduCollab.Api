using System.ComponentModel.DataAnnotations;
using Space.Contracts.Validation;

namespace Space.Contracts.Requests.Users
{
    public class ConfirmPasswordResetRequest
    {
        [Required]
        [EmailAddress]
        [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailError)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordError)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
