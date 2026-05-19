using System.ComponentModel.DataAnnotations;
using EduCollab.Contracts.Validation;

namespace EduCollab.Contracts.Requests.Users
{
    public class PasswordResetRequest
    {
        [Required]
        [EmailAddress]
        [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailError)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
    }
}
