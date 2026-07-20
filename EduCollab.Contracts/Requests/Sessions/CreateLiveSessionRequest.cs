using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Sessions
{
    public class CreateLiveSessionRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Scene to host. Exactly one of <see cref="SceneId"/> or <see cref="FlowId"/> is required.
        /// </summary>
        public int? SceneId { get; set; }

        /// <summary>
        /// Flow to host. Exactly one of <see cref="SceneId"/> or <see cref="FlowId"/> is required.
        /// </summary>
        public int? FlowId { get; set; }

        public List<int>? GroupIds { get; set; }

        public List<int>? UserIds { get; set; }

        public bool IncludeAssets { get; set; }

        public bool AllowGuestLink { get; set; }

        /// <summary>
        /// Default Colyseus role for participants: viewer, editor, or presenter.
        /// </summary>
        [MaxLength(32)]
        public string? DefaultRole { get; set; }
    }
}
