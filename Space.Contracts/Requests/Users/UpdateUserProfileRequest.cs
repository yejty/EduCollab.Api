using System.ComponentModel.DataAnnotations;

namespace Space.Contracts.Requests.Users
{
    public class UpdateUserProfileRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
