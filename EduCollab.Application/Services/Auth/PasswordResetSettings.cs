namespace EduCollab.Application.Services.Auth
{
    public sealed class PasswordResetSettings
    {
        public const string SectionName = "PasswordReset";

        /// <summary>
        /// Lifetime of a password reset link/token.
        /// </summary>
        public int TokenExpirationHours { get; set; } = 1;

        /// <summary>
        /// When true and the host environment is Development, logs the plaintext reset token (for local testing without email).
        /// </summary>
        public bool LogPlaintextTokenInDevelopment { get; set; }
    }
}
