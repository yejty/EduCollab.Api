namespace EduCollab.Application.Models.Workspaces
{
    /// <summary>
    /// Role of a user within a workspace. Persisted as the enum name in <c>WorkspaceMembers.Role</c>.
    /// </summary>
    public enum WorkspaceRole
    {
        Owner = 0,
        Admin = 1,
        Member = 2,
    }
}
