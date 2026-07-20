namespace EduCollab.Application.Models
{
    public sealed class SessionGuestLink
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Opaque share token (capability URL secret). Stored so hosts can re-read the guest link.
        /// </summary>
        public string TokenPlaintext { get; set; } = string.Empty;

        public DateTime? ExpiresAtUtc { get; set; }

        public int? MaxUses { get; set; }

        public int UseCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
