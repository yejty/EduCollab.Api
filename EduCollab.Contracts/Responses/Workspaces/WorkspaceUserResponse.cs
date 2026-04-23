namespace EduCollab.Contracts.Responses.Workspaces
{
    /// <summary>
    /// A user that belongs to a workspace, including their membership role.
    /// </summary>
    public class WorkspaceUserResponse
    {
        public long Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        /// <summary>Workspace role (e.g. Owner, Admin, Member).</summary>
        public string Role { get; set; } = string.Empty;
    }
}
