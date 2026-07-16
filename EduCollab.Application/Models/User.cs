using System.ComponentModel.DataAnnotations;

namespace EduCollab.Application.Models
{
    public class User
    {
        public int Id { get; set; }

        [MaxLength(200), Required]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? EmailConfirmedAtUtc { get; set; }

        /// <summary>
        /// Active workspace for current-workspace API routes. Membership is stored in <see cref="WorkspaceMember"/>.
        /// </summary>
        public int? WorkspaceId { get; set; }

        /// <summary>
        /// All workspace ids the user belongs to. Populated when loading user profiles for API responses.
        /// </summary>
        public List<int> MemberWorkspaceIds { get; set; } = [];

        /// <summary>
        /// Platform-wide administrator (not a workspace role).
        /// </summary>
        public bool IsPlatformAdmin { get; set; }
    }
}
