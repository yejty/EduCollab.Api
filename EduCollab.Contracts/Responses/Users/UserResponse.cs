using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Responses.Users
{
    public class UserResponse
    {
        public long Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>When null, the user is not assigned to a workspace yet.</summary>
        public int? WorkspaceId { get; set; }
    }
}
