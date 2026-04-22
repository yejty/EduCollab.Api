using System.ComponentModel.DataAnnotations;

namespace EduCollab.Application.Models.Users
{
    public class User
    {
        [MaxLength(100), Required]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100), Required]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress, Required]
        public string? Email { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
