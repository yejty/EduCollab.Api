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

        /// <summary>
        /// Group the invitee joins. Membership grants access to this group and all of its subgroups.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "GroupId is required.")]
        public int GroupId { get; set; }

        [Required]
        [MinLength(1)]
        public List<string> Parameters { get; set; } = new();
    }
}
