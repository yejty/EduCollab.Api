using EduCollab.Contracts.Workspaces;

namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspaceMemberResponse
    {
        public int UserId { get; set; }

        public WorkspaceRole Role { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }
    }
}
