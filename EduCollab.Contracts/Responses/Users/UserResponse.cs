namespace EduCollab.Contracts.Responses.Users
{
    public class UserResponse
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>All workspace ids the user belongs to.</summary>
        public List<int> WorkspaceIds { get; set; } = [];
    }
}
