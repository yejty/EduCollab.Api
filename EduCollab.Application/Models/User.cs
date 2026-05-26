using System.ComponentModel.DataAnnotations;

namespace EduCollab.Application.Models
{
    public class User
    {
        public int Id { get; set; }

        [MaxLength(100), Required]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100), Required]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        public DateTime? EmailConfirmedAtUtc { get; set; }

        /// <summary>
        /// At most one workspace per user: when set, this user belongs only to this workspace.
        /// </summary>
        public int? WorkspaceId { get; set; }
    }
}
