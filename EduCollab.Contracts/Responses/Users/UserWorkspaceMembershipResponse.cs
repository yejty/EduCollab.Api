namespace EduCollab.Contracts.Responses.Users
{
    public class UserWorkspaceMembershipResponse
    {
        public int WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
