using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Assets
{
    public class CreateAssetRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string AssetType { get; set; } = string.Empty;

        public int? FolderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string StorageProvider { get; set; } = string.Empty;

        [Required]
        public string StorageKey { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? MimeType { get; set; }

        public long? SizeInBytes { get; set; }
    }
}
