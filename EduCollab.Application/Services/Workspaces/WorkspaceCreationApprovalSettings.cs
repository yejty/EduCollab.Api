namespace EduCollab.Application.Services.Workspaces
{
    public sealed class WorkspaceCreationApprovalSettings
    {
        public const string SectionName = "WorkspaceCreationApproval";

        /// <summary>
        /// Lifetime of a workspace creation approval token (default 7 days).
        /// </summary>
        public int TokenExpirationHours { get; set; } = 168;

        /// <summary>
        /// Frontend URL where the user completes workspace creation (no trailing slash).
        /// The email link is <c>{FrontendCreateUrl}?token=…</c>.
        /// </summary>
        public string FrontendCreateUrl { get; set; } = string.Empty;

        /// <summary>
        /// API base URL for one-click admin review links in email (no trailing slash).
        /// Approve link: <c>{AdminReviewUrlBase}/{requestId}/{token}/approve</c>.
        /// Deny link: <c>{AdminReviewUrlBase}/{requestId}/{token}/deny</c>.
        /// </summary>
        public string AdminReviewUrlBase { get; set; } = string.Empty;

        /// <summary>
        /// Lifetime of admin review links sent by email (default 7 days).
        /// </summary>
        public int AdminReviewTokenExpirationHours { get; set; } = 168;

        /// <summary>
        /// When true and the host environment is Development, logs the plaintext approval token.
        /// </summary>
        public bool LogPlaintextTokenInDevelopment { get; set; }
    }
}
