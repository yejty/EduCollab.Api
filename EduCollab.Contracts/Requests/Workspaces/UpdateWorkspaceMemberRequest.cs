using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Workspaces
{
    public class UpdateWorkspaceMemberRequest
    {
        [Required]
        [MinLength(1)]
        public List<string> Presets { get; set; } = new();
    }
}
