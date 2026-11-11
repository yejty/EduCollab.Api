using System.ComponentModel.DataAnnotations;

namespace Space.Contracts.Requests.Users
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
