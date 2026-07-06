using System.ComponentModel.DataAnnotations;
using EduCollab.Contracts.Validation;

namespace EduCollab.Contracts.Requests.Users
{
    public class InviteUserRequest
    {
        [Required]
        [EmailAddress]
        [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailError)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<string> Presets { get; set; } = new();
    }
}
