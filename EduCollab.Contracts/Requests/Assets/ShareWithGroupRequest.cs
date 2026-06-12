using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Assets
{
    public class ShareWithGroupRequest
    {
        [Range(1, int.MaxValue)]
        public int GroupId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;
    }
}
