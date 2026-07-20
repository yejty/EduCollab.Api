namespace EduCollab.Application.Services.Sessions
{
    public sealed class SessionJoinSettings
    {
        public const string SectionName = "SessionJoin";

        /// <summary>
        /// HMAC secret shared with the Colyseus collab-server for join-ticket verification.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Issuer claim written into join tickets.
        /// </summary>
        public string Issuer { get; set; } = "EduCollab.Api";

        /// <summary>
        /// Audience claim written into join tickets.
        /// </summary>
        public string Audience { get; set; } = "EduCollab.CollabServer";

        /// <summary>
        /// Join ticket lifetime in minutes.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 10;

        /// <summary>
        /// Public Colyseus WebSocket endpoint returned to clients (e.g. wss://collab.example.com).
        /// </summary>
        public string ColyseusEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Frontend base path for guest join links (no trailing slash).
        /// The share URL is <c>{FrontendGuestJoinUrl}/{token}</c>.
        /// </summary>
        public string FrontendGuestJoinUrl { get; set; } = string.Empty;

        /// <summary>
        /// Lifetime of a guest invite link. Null/0 means no expiry.
        /// </summary>
        public int GuestLinkExpirationHours { get; set; } = 168;

        /// <summary>
        /// Active sessions with no room activity older than this are marked Ended by cleanup.
        /// </summary>
        public int AbandonedSessionIdleHours { get; set; } = 24;
    }
}
