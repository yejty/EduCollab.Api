using System.ComponentModel.DataAnnotations;
using EduCollab.Contracts.Validation;

namespace EduCollab.Contracts.Requests.Users
{
    public class RegisterUserRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailError)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordError)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordError)]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
