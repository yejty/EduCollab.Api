using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Workspaces
{
    public class RequestWorkspaceCreationRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Optional workspace type used by clients to select UI/app functionality (e.g. classroom, lab).
        /// </summary>
        [MaxLength(50)]
        public string? Type { get; set; }
    }
}
