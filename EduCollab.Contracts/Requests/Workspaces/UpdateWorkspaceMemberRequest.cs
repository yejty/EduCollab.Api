using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Workspaces
{
    public class UpdateWorkspaceMemberRequest
    {
        [Required]
        public string Preset { get; set; } = string.Empty;
    }
}
