using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Responses.Users
{
    public class UserResponse
    {
        public long Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>Active workspace id for current-workspace routes, or null when none is selected.</summary>
        public int? WorkspaceId { get; set; }
    }
}
