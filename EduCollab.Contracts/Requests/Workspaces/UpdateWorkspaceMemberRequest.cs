using EduCollab.Contracts.Workspaces;

namespace EduCollab.Contracts.Requests.Workspaces
{
    public class UpdateWorkspaceMemberRequest
    {
        public int UserId { get; set; }
        public WorkspaceRole Role { get; set; }
    }
}
