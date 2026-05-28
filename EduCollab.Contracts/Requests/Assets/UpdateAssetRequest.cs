using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Assets
{
    public class UpdateAssetRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? FolderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AssetType { get; set; } = string.Empty;

        [Required]
        [Url]
        public string StorageUrl { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Version { get; set; }
    }
}
