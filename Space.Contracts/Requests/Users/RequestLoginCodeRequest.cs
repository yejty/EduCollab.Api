using System.ComponentModel.DataAnnotations;
using Space.Contracts.Validation;

namespace Space.Contracts.Requests.Users
{
    public sealed class RequestLoginCodeRequest
    {
        [Required]
        [EmailAddress]
        [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailError)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
    }
}
