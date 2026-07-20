namespace EduCollab.Contracts.Responses.Sessions
{
    public class LiveSessionResponse
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public int HostUserId { get; set; }

        public string? HostDisplayName { get; set; }

        public int? SceneId { get; set; }

        public string? SceneName { get; set; }

        public int? FlowId { get; set; }

        public string? FlowName { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IncludeAssets { get; set; }

        public bool AllowGuestLink { get; set; }

        public string? GuestLinkUrl { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string DefaultRole { get; set; } = string.Empty;

        public List<int> GroupIds { get; set; } = [];

        public List<int> UserIds { get; set; } = [];

        public string? EffectiveRole { get; set; }

        public bool CanJoin { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? StartedAtUtc { get; set; }

        public DateTime? EndedAtUtc { get; set; }
    }
}
