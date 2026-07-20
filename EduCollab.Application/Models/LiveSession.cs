namespace EduCollab.Application.Models
{
    public static class LiveSessionStatuses
    {
        public const string Pending = "Pending";
        public const string Active = "Active";
        public const string Ended = "Ended";
    }

    public static class LiveSessionRoles
    {
        public const string Host = "host";
        public const string Participant = "participant";
        public const string Guest = "guest";
    }

    public static class ColyseusRoles
    {
        public const string Host = "host";
        public const string Editor = "editor";
        public const string Presenter = "presenter";
        public const string Viewer = "viewer";
    }

    public class LiveSession
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public int HostUserId { get; set; }

        public int? SceneId { get; set; }

        public int? FlowId { get; set; }

        public string Status { get; set; } = LiveSessionStatuses.Pending;

        public bool IncludeAssets { get; set; }

        public bool AllowGuestLink { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string DefaultRole { get; set; } = ColyseusRoles.Viewer;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? StartedAtUtc { get; set; }

        public DateTime? EndedAtUtc { get; set; }

        public List<int> GroupIds { get; set; } = [];

        public List<int> UserIds { get; set; } = [];

        public string? HostDisplayName { get; set; }

        public string? SceneName { get; set; }

        public string? FlowName { get; set; }

        public string? EffectiveRole { get; set; }

        public bool CanJoin { get; set; }

        /// <summary>
        /// Guest join URL for hosts when <see cref="AllowGuestLink"/> is enabled.
        /// </summary>
        public string? GuestLinkUrl { get; set; }
    }
}
