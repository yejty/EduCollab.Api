using EduCollab.Application.Models;

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

        public static EmailContent WorkspaceCreationRequestAdminNotification(
            string requesterFirstName,
            string requesterLastName,
            string requesterEmail,
            string workspaceName,
            string? description,
            string? approveUrl,
            string? denyUrl)
        {
            var nameEncoded = EmailHtmlBuilder.Encode($"{requesterFirstName} {requesterLastName}".Trim());
            var emailEncoded = EmailHtmlBuilder.Encode(requesterEmail);
            var workspaceEncoded = EmailHtmlBuilder.Encode(workspaceName);
            var descriptionText = string.IsNullOrWhiteSpace(description) ? "(none)" : description.Trim();
            var hasReviewLinks = !string.IsNullOrWhiteSpace(approveUrl) && !string.IsNullOrWhiteSpace(denyUrl);

            var plain =
                "A new workspace creation request is waiting for platform admin review." + Environment.NewLine + Environment.NewLine +
                $"Requester: {requesterFirstName} {requesterLastName} ({requesterEmail})" + Environment.NewLine +
                $"Requested workspace name: {workspaceName}" + Environment.NewLine +
                $"Description: {descriptionText}";

            if (hasReviewLinks)
            {
                plain +=
                    Environment.NewLine + Environment.NewLine +
                    "Approve this request:" + Environment.NewLine +
                    approveUrl + Environment.NewLine + Environment.NewLine +
                    "Deny this request:" + Environment.NewLine +
                    denyUrl + Environment.NewLine + Environment.NewLine +
                    "The requester will receive an email after you approve or deny.";
            }

            var innerHtml =
                EmailHtmlBuilder.Paragraph("A new workspace creation request is waiting for your review.") +
                EmailHtmlBuilder.Paragraph($"Requester: <strong>{nameEncoded}</strong> (<a href=\"mailto:{emailEncoded}\">{emailEncoded}</a>)") +
                EmailHtmlBuilder.Paragraph($"Requested workspace name: <strong>{workspaceEncoded}</strong>") +
                EmailHtmlBuilder.Paragraph($"Description: {EmailHtmlBuilder.Encode(descriptionText)}");

            if (hasReviewLinks)
            {
                innerHtml +=
                    EmailHtmlBuilder.Muted("The requester will receive an email after you approve or deny.") +
                    EmailHtmlBuilder.ActionList(new[]
                    {
                        new NotificationAction("Approve request", approveUrl!),
                        new NotificationAction("Deny request", denyUrl!, NotificationActionStyle.Danger),
                    }) +
                    EmailHtmlBuilder.UrlFallback(approveUrl!) +
                    EmailHtmlBuilder.Label("Deny link") +
                    "<p style=\"margin:0 0 16px;font-size:13px;line-height:1.45;color:#9aa4b2;word-break:break-all;\">" +
                    EmailHtmlBuilder.Encode(denyUrl!) + "</p>";
            }
            else
            {
                innerHtml += EmailHtmlBuilder.FinePrint(
                    "Set WorkspaceCreationApproval:AdminReviewUrlBase to receive Approve and Deny buttons in this email.");
            }

            return new EmailContent(
                $"New {BrandName} workspace creation request",
                plain,
                EmailHtmlBuilder.WrapDocument(BrandName, "Workspace creation request", innerHtml));
        }

        public static EmailContent WorkspaceCreationApproved(
            string requesterFirstName,
            string workspaceName,
            string? createUrl,
            string plaintextTokenFallback,
            int validForHours)
        {
            var validityText = FormatValidityDaysFromHours(validForHours);
            var first = EmailHtmlBuilder.Encode(requesterFirstName);
            var workspaceEncoded = EmailHtmlBuilder.Encode(workspaceName);

            if (!string.IsNullOrWhiteSpace(createUrl))
            {
                var plain =
                    $"Hello {requesterFirstName}," + Environment.NewLine + Environment.NewLine +
                    $"Your request to create the workspace \"{workspaceName}\" was approved." + Environment.NewLine + Environment.NewLine +
                    $"Use this link to create your workspace (valid for {validityText}):" + Environment.NewLine + Environment.NewLine +
                    createUrl + Environment.NewLine + Environment.NewLine +
                    $"When creating the workspace, include this approval token: {plaintextTokenFallback}";

                var innerHtml =
                    EmailHtmlBuilder.Paragraph($"Hello <strong>{first}</strong>,") +
                    EmailHtmlBuilder.Paragraph($"Your request to create <strong>{workspaceEncoded}</strong> was approved.") +
                    EmailHtmlBuilder.Muted($"This link and token expire in {validityText}.") +
                    EmailHtmlBuilder.ActionList(new[] { new NotificationAction("Create workspace", createUrl) }) +
                    EmailHtmlBuilder.Label("Approval token") +
                    EmailHtmlBuilder.CodeBlock(plaintextTokenFallback);

                return new EmailContent(
                    $"Your {BrandName} workspace request was approved",
                    plain,
                    EmailHtmlBuilder.WrapDocument(BrandName, "Workspace request approved", innerHtml));
            }

            var plainTokenOnly =
                $"Hello {requesterFirstName}," + Environment.NewLine + Environment.NewLine +
                $"Your request to create the workspace \"{workspaceName}\" was approved." + Environment.NewLine + Environment.NewLine +
                $"Use this approval token when creating your workspace (valid for {validityText}):" + Environment.NewLine + Environment.NewLine +
                plaintextTokenFallback;

            var innerTokenOnly =
                EmailHtmlBuilder.Paragraph($"Hello <strong>{first}</strong>,") +
                EmailHtmlBuilder.Paragraph($"Your request to create <strong>{workspaceEncoded}</strong> was approved.") +
                EmailHtmlBuilder.Label($"Approval token (expires in {validityText})") +
                EmailHtmlBuilder.CodeBlock(plaintextTokenFallback);

            return new EmailContent(
                $"Your {BrandName} workspace request was approved",
                plainTokenOnly,
                EmailHtmlBuilder.WrapDocument(BrandName, "Workspace request approved", innerTokenOnly));
        }

        public static EmailContent WorkspaceCreationDenied(string requesterFirstName, string workspaceName, string? reason)
        {
            var first = EmailHtmlBuilder.Encode(requesterFirstName);
            var workspaceEncoded = EmailHtmlBuilder.Encode(workspaceName);
            var reasonText = string.IsNullOrWhiteSpace(reason)
                ? "No reason was provided."
                : reason.Trim();

            var plain =
                $"Hello {requesterFirstName}," + Environment.NewLine + Environment.NewLine +
                $"Your request to create the workspace \"{workspaceName}\" was denied." + Environment.NewLine + Environment.NewLine +
                reasonText;

            var innerHtml =
                EmailHtmlBuilder.Paragraph($"Hello <strong>{first}</strong>,") +
                EmailHtmlBuilder.Paragraph($"Your request to create <strong>{workspaceEncoded}</strong> was denied.") +
                EmailHtmlBuilder.WarningCallout(EmailHtmlBuilder.Encode(reasonText));

            return new EmailContent(
                $"Your {BrandName} workspace request was denied",
                plain,
                EmailHtmlBuilder.WrapDocument(BrandName, "Workspace request denied", innerHtml));
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
