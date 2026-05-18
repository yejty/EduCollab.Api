using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Application.Services.Auth
{
    public sealed class EmailConfirmationSettings
    {
        public const string SectionName = "EmailConfirmation";

        /// <summary>
        /// Lifetime of the email confirmation link/token.
        /// </summary>
        public int TokenExpirationHours { get; set; } = 24;

        /// <summary>
        /// Frontend route opened from the confirmation email; query params <c>email</c> and <c>token</c> are appended.
        /// </summary>
        public string FrontendConfirmUrl { get; set; } = string.Empty;

        /// <summary>
        /// When true and the host environment is Development, logs the plaintext confirmation token for local testing.
        /// </summary>
        public bool LogPlaintextTokenInDevelopment { get; set; }
    }
}
