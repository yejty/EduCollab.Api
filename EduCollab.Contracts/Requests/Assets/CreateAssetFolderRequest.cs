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
        /// Optional group to share with on create. When omitted, the folder is private to the creator and workspace owner.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? GroupId { get; set; }
    }
}
