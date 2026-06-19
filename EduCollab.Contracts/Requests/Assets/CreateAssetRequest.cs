using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Assets
{
    public class CreateAssetRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? FolderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AssetType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Version { get; set; }

        /// <summary>
        /// Group that receives access to this asset when it is created.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "GroupId is required.")]
        public int GroupId { get; set; }
    }
}
