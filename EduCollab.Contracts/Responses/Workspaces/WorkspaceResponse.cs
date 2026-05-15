using EduCollab.Contracts.Workspaces;

namespace EduCollab.Contracts.Responses.Workspaces
{
    public class WorkspaceResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>
        /// The authenticated user's role in this workspace, when known.
        /// </summary>
        public WorkspaceRole? CurrentUserRole { get; set; }

        public int UsersCount { get; set; }
    }
}

