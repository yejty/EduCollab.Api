using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Workspaces
{
    public class UpdateWorkspaceMemberRequest
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
