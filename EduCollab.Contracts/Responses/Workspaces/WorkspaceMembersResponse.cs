namespace EduCollab.Contracts.Responses.Workspaces
{
    /// <summary>
    /// Collection of members for a workspace (not the same shape as global user profile).
    /// </summary>
    public sealed class WorkspaceMembersResponse
    {
        public List<WorkspaceMemberResponse> Members { get; set; } = new();
    }
}
