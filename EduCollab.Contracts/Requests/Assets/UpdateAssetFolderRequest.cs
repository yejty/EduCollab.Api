using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Assets
{
    public class UpdateAssetFolderRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int? ParentFolderId { get; set; }
    }
}
