using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Sessions
{
    public class PublicSessionJoinRequest
    {
        [Required]
        public string GuestToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;
    }
}
