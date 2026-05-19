using System.ComponentModel.DataAnnotations;
using EduCollab.Contracts.Validation;

namespace EduCollab.Contracts.Requests.Users
{
    public class ChangePasswordRequest
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordError)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
        
    }
}
