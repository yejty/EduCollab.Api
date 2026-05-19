using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Services.Notifications
{
    /// <summary>
    /// Transactional email bodies (plain + HTML) with a shared layout
    /// aligned to <c>Helpers/design-tokens.md</c> and <c>Helpers/index.css</c>.
    /// </summary>
    public static class EduCollabEmailTemplates
    {
        private const string BrandName = "EduCollab";

        public static EmailContent ProfileUpdated(User user)
        {
            var first = EmailHtmlBuilder.Encode(user.FirstName);
            var plain =
                $"Hello {user.FirstName}," + Environment.NewLine + Environment.NewLine +
                "Your profile details were updated in EduCollab." + Environment.NewLine + Environment.NewLine +
                "If you did not make this change, contact support immediately.";

            var innerHtml =
                EmailHtmlBuilder.Paragraph($"Hello <strong>{first}</strong>,") +
                EmailHtmlBuilder.Paragraph("Your profile details were updated.") +
                EmailHtmlBuilder.WarningCallout("<strong>Did not make this change?</strong> Contact support immediately.");

            return new EmailContent(
                $"Your {BrandName} profile was updated",
                plain,
                EmailHtmlBuilder.WrapDocument(BrandName, "Profile updated", innerHtml));
        }

        public static EmailContent PasswordChanged()
        {
            var plain =
                "Your EduCollab password was changed." + Environment.NewLine + Environment.NewLine +
                "If you did not make this change, reset your password immediately and contact support.";

            var innerHtml =
                EmailHtmlBuilder.Paragraph("Your password was successfully changed.") +
                EmailHtmlBuilder.WarningCallout(
                    "<strong>Did not make this change?</strong> Reset your password right away and contact support.");

            return new EmailContent(
                $"Your {BrandName} password was changed",
                plain,
                EmailHtmlBuilder.WrapDocument(BrandName, "Password changed", innerHtml));
        }

        public static EmailContent PasswordResetRequest(string resetToken, int validForMinutes)
        {
            var validityText = FormatValidityMinutes(validForMinutes);
            var plain =
                "You requested a password reset for your EduCollab account." + Environment.NewLine + Environment.NewLine +
                $"Use this token in the app (valid for {validityText}):" + Environment.NewLine + Environment.NewLine +
                resetToken + Environment.NewLine + Environment.NewLine +
                "If you did not request this, you can ignore this email.";

            var innerHtml =
                EmailHtmlBuilder.Paragraph("You requested a password reset. Use the token below in the app to set a new password.") +
                EmailHtmlBuilder.Label($"Token (expires in {validityText})") +
                EmailHtmlBuilder.CodeBlock(resetToken) +
                EmailHtmlBuilder.FinePrint("If you did not request this, you can ignore this email.");

            return new EmailContent(
                $"Reset your {BrandName} password",
                plain,
                EmailHtmlBuilder.WrapDocument(BrandName, "Password reset", innerHtml));
        }

        public static EmailContent WorkspaceInvitation(
            string workspaceName,
            string? acceptUrl,
            string plaintextTokenFallback,
            int validForHours)
        {
            var validityText = FormatValidityDaysFromHours(validForHours);
            var hasWorkspaceName = !string.IsNullOrWhiteSpace(workspaceName);
            var workspaceNameEncoded = EmailHtmlBuilder.Encode(workspaceName);
            var workspaceLabel = hasWorkspaceName ? $"\"{workspaceName}\"" : "a workspace";
            var inviteIntroHtml = hasWorkspaceName
                ? "You have been invited to join the workspace <strong>" + workspaceNameEncoded + "</strong> on EduCollab."
                : "You have been invited to join a workspace on EduCollab.";

            if (!string.IsNullOrWhiteSpace(acceptUrl))
            {
                var plain =
                    $"You have been invited to join the workspace {workspaceLabel} on EduCollab." + Environment.NewLine + Environment.NewLine +
                    $"Open this link to accept (valid for {validityText}):" + Environment.NewLine + Environment.NewLine +
                    acceptUrl + Environment.NewLine + Environment.NewLine +
                    "If you did not expect this invitation, you can ignore this email.";

                var innerHtml =
                    EmailHtmlBuilder.Paragraph(inviteIntroHtml) +
                    EmailHtmlBuilder.Muted("This link expires in " + validityText + ".") +
                    EmailHtmlBuilder.ActionList(new[]
                    {
                        new NotificationAction("Accept invitation", acceptUrl)
                    }) +
                    EmailHtmlBuilder.UrlFallback(acceptUrl) +
                    EmailHtmlBuilder.FinePrint("If you did not expect this invitation, you can ignore this email.");

                var subject = string.IsNullOrWhiteSpace(workspaceName)
                    ? $"Invitation to a {BrandName} workspace"
                    : $"Invitation to join {workspaceName} on {BrandName}";

                return new EmailContent(
                    subject,
                    plain,
                    EmailHtmlBuilder.WrapDocument(BrandName, "Workspace invitation", innerHtml));
            }

            var plainTokenOnly =
                $"You have been invited to join the workspace {workspaceLabel} on EduCollab." + Environment.NewLine + Environment.NewLine +
                $"Use this invitation token (valid for {validityText}):" + Environment.NewLine + Environment.NewLine +
                plaintextTokenFallback + Environment.NewLine + Environment.NewLine +
                "Configure WorkspaceInvitation:FrontendAcceptUrl to send a clickable link instead.";

            var innerTokenOnly =
                EmailHtmlBuilder.Paragraph(inviteIntroHtml) +
                EmailHtmlBuilder.Label($"Invitation token (expires in {validityText})") +
                EmailHtmlBuilder.CodeBlock(plaintextTokenFallback) +
                EmailHtmlBuilder.FinePrint("Set WorkspaceInvitation:FrontendAcceptUrl to receive an accept button link instead.");

            var subjectFallback = string.IsNullOrWhiteSpace(workspaceName)
                ? $"Invitation to a {BrandName} workspace"
                : $"Invitation to join {workspaceName} on {BrandName}";

            return new EmailContent(
                subjectFallback,
                plainTokenOnly,
                EmailHtmlBuilder.WrapDocument(BrandName, "Workspace invitation", innerTokenOnly));
        }

        public static EmailContent EmailConfirmation(string confirmUrl, string plaintextTokenFallback, int validForHours)
        {
            var validityText = FormatValidityDaysFromHours(validForHours);

            if (!string.IsNullOrWhiteSpace(confirmUrl))
            {
                var plain =
                    "Welcome to EduCollab. Confirm your email address to finish setting up your account." + Environment.NewLine + Environment.NewLine +
                    $"Open this link (valid for {validityText}):" + Environment.NewLine + Environment.NewLine +
                    confirmUrl + Environment.NewLine + Environment.NewLine +
                    "If you did not register, you can ignore this email.";

                var innerHtml =
                    EmailHtmlBuilder.Paragraph("Thanks for signing up. Confirm your email address to activate your account.") +
                    EmailHtmlBuilder.Muted("This link expires in " + validityText + ".") +
                    EmailHtmlBuilder.ActionList(new[]
                    {
                        new NotificationAction("Confirm email", confirmUrl)
                    }) +
                    EmailHtmlBuilder.UrlFallback(confirmUrl) +
                    EmailHtmlBuilder.FinePrint("If you did not register, you can ignore this email.");

                return new EmailContent(
                    $"Confirm your {BrandName} email",
                    plain,
                    EmailHtmlBuilder.WrapDocument(BrandName, "Confirm your email", innerHtml));
            }

            var plainTokenOnly =
                "Welcome to EduCollab. Confirm your email address using the token below (valid for " + validityText + "):" +
                Environment.NewLine + Environment.NewLine +
                plaintextTokenFallback + Environment.NewLine + Environment.NewLine +
                "Configure FrontendConfirmUrl in EmailConfirmation settings to send a clickable link instead.";

            var innerTokenOnly =
                EmailHtmlBuilder.Paragraph("Thanks for signing up. Confirm your email using the token below.") +
                EmailHtmlBuilder.Label($"Token (expires in {validityText})") +
                EmailHtmlBuilder.CodeBlock(plaintextTokenFallback) +
                EmailHtmlBuilder.FinePrint("Set EmailConfirmation:FrontendConfirmUrl to receive a button link instead.");

            return new EmailContent(
                $"Confirm your {BrandName} email",
                plainTokenOnly,
                EmailHtmlBuilder.WrapDocument(BrandName, "Confirm your email", innerTokenOnly));
        }

        public static EmailContent LoginCode(string code, int validForMinutes)
        {
            var validityText = FormatValidityMinutes(validForMinutes);
            var plain =
                "Use this 6-digit sign-in code to log in to EduCollab." + Environment.NewLine + Environment.NewLine +
                $"Code (expires in {validityText}):" + Environment.NewLine + Environment.NewLine +
                code + Environment.NewLine + Environment.NewLine +
                "If you did not request this code, you can ignore this email.";

            var innerHtml =
                EmailHtmlBuilder.Paragraph("Use this one-time code to sign in.") +
                EmailHtmlBuilder.Label($"6-digit code (expires in {validityText})") +
                EmailHtmlBuilder.CodeBlock(code, large: true) +
                EmailHtmlBuilder.FinePrint("If you did not request this code, you can ignore this email.");

            return new EmailContent(
                $"Your {BrandName} sign-in code",
                plain,
                EmailHtmlBuilder.WrapDocument(BrandName, "Sign-in code", innerHtml));
        }

        private static string FormatValidityDaysFromHours(int validForHours)
        {
            if (validForHours <= 0)
                return "1 day";

            var days = (int)Math.Ceiling(validForHours / 24.0);
            return days == 1 ? "1 day" : $"{days} days";
        }

        private static string FormatValidityMinutes(int validForMinutes)
        {
            if (validForMinutes <= 0)
                return "1 minute";

            return validForMinutes == 1 ? "1 minute" : $"{validForMinutes} minutes";
        }
    }
}
