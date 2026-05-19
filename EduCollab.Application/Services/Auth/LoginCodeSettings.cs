namespace EduCollab.Application.Services.Auth
{
    public sealed class LoginCodeSettings
    {
        public const string SectionName = "LoginCode";

        /// <summary>
        /// Lifetime of a one-time sign-in code sent by email.
        /// </summary>
        public int CodeExpirationMinutes { get; set; } = 3;

        /// <summary>
        /// When true and the host environment is Development, logs the plaintext sign-in code for local testing.
        /// </summary>
        public bool LogPlaintextCodeInDevelopment { get; set; }

        /// <summary>
        /// Maximum number of wrong confirmation attempts before the code is invalidated.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;
    }
}
