namespace EduCollab.Contracts.Responses.Users
{
    public class UserWorkspacesResponse
    {
        public List<UserWorkspaceMembershipResponse> Workspaces { get; set; } = new();
    }
}
