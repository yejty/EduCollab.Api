namespace EduCollab.Application.Services.Workspaces
{
    public sealed class WorkspaceInvitationSettings
    {
        public const string SectionName = "WorkspaceInvitation";

        /// <summary>
        /// Lifetime of a workspace invitation token.
        /// </summary>
        public int TokenExpirationHours { get; set; } = 168;

        /// <summary>
        /// When true and the host environment is Development, logs the plaintext invitation token (for local testing without email).
        /// </summary>
        public bool LogPlaintextTokenInDevelopment { get; set; }
    }
}
