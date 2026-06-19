using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Assets
{
    public class CreateAssetFolderRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int? ParentFolderId { get; set; }

        /// <summary>
        /// Group that receives access to this folder when it is created.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "GroupId is required.")]
        public int GroupId { get; set; }
    }
}
