namespace EduCollab.Application.Models
{
    /// <summary>
    /// Role of a user within a workspace. Inherited in groups — group membership does not define a separate role.
    /// Persisted as the enum name in <c>WorkspaceMembers.Role</c>.
    /// </summary>
    public enum WorkspaceRole
    {
        Owner = 0,
        Manager = 1,
        Creator = 2,
        Viewer = 3,
    }
}
