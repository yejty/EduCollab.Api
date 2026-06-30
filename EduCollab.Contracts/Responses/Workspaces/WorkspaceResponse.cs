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
        public string? CurrentUserRole { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int CreatedByUserId { get; set; }

        public bool IsArchived { get; set; }
    }
}

