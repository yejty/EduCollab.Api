namespace EduCollab.Application.Services.Notifications
{
    public sealed class EmailSettings
    {
        public const string SectionName = "Email";

        /// <summary>
        /// When false, outbound email is skipped (logged at Information level).
        /// </summary>
        public bool Enabled { get; set; }

        public string? SmtpHost { get; set; }

        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// When true, uses TLS (typically STARTTLS on port 587).
        /// </summary>
        public bool UseSsl { get; set; } = true;

        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string FromAddress { get; set; } = "noreply@localhost";

        public string FromDisplayName { get; set; } = "EduCollab";
    }
}
