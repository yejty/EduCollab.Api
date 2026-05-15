using System.Net;
using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Services.Notifications
{
    /// <summary>
    /// Transactional email bodies (plain + HTML) with a shared layout.
    /// </summary>
    public static class EduCollabEmailTemplates
    {
        private const string BrandName = "EduCollab";

        public static EmailContent ProfileUpdated(User user)
        {
            var first = WebUtility.HtmlEncode(user.FirstName);
            var plain =
                $"Hello {user.FirstName}," + Environment.NewLine + Environment.NewLine +
                "Your profile details were updated in EduCollab." + Environment.NewLine + Environment.NewLine +
                "If you did not make this change, contact support immediately.";

            var innerHtml =
                $"<p style=\"margin:0 0 16px;font-size:15px;line-height:1.5;color:#374151;\">Hello <strong>{first}</strong>,</p>" +
                "<p style=\"margin:0 0 16px;font-size:15px;line-height:1.5;color:#374151;\">" +
                "Your profile details were updated.</p>" +
                "<p style=\"margin:0;font-size:15px;line-height:1.5;color:#b45309;background:#fffbeb;padding:12px 14px;border-radius:6px;border:1px solid #fcd34d;\">" +
                "<strong>Did not make this change?</strong> Contact support immediately.</p>";

            return new EmailContent(
                $"Your {BrandName} profile was updated",
                plain,
                WrapLayout("Profile updated", innerHtml));
        }

        public static EmailContent PasswordChanged()
        {
            var plain =
                "Your EduCollab password was changed." + Environment.NewLine + Environment.NewLine +
                "If you did not make this change, reset your password immediately and contact support.";

            var innerHtml =
                "<p style=\"margin:0 0 16px;font-size:15px;line-height:1.5;color:#374151;\">" +
                "Your password was successfully changed.</p>" +
                "<p style=\"margin:0;font-size:15px;line-height:1.5;color:#b45309;background:#fffbeb;padding:12px 14px;border-radius:6px;border:1px solid #fcd34d;\">" +
                "<strong>Did not make this change?</strong> Reset your password right away and contact support.</p>";

            return new EmailContent(
                $"Your {BrandName} password was changed",
                plain,
                WrapLayout("Password changed", innerHtml));
        }

        public static EmailContent PasswordResetRequest(string resetToken, int validForHours)
        {
            var tokenEncoded = WebUtility.HtmlEncode(resetToken);
            var plain =
                "You requested a password reset for your EduCollab account." + Environment.NewLine + Environment.NewLine +
                $"Use this token in the app (valid for {validForHours} hour(s)):" + Environment.NewLine + Environment.NewLine +
                resetToken + Environment.NewLine + Environment.NewLine +
                "If you did not request this, you can ignore this email.";

            var innerHtml =
                "<p style=\"margin:0 0 16px;font-size:15px;line-height:1.5;color:#374151;\">" +
                "You requested a password reset. Use the token below in the app to set a new password.</p>" +
                "<p style=\"margin:0 0 8px;font-size:13px;color:#6b7280;\">Token (expires in " + validForHours + " hour(s))</p>" +
                "<pre style=\"margin:0 0 20px;padding:14px 16px;background:#f3f4f6;border-radius:8px;border:1px solid #e5e7eb;" +
                "font-family:ui-monospace,Consolas,monospace;font-size:13px;line-height:1.45;color:#111827;white-space:pre-wrap;word-break:break-all;\">" +
                tokenEncoded + "</pre>" +
                "<p style=\"margin:0;font-size:14px;line-height:1.5;color:#6b7280;\">If you did not request this, you can ignore this email.</p>";

            return new EmailContent(
                $"Reset your {BrandName} password",
                plain,
                WrapLayout("Password reset", innerHtml));
        }

        public static EmailContent WorkspaceInvitation(int workspaceId, string invitationToken, int validForHours)
        {
            var workspaceEncoded = WebUtility.HtmlEncode(workspaceId.ToString());
            var tokenEncoded = WebUtility.HtmlEncode(invitationToken);
            var plain =
                "You have been invited to join a workspace on EduCollab." + Environment.NewLine + Environment.NewLine +
                $"Workspace ID: {workspaceId}" + Environment.NewLine + Environment.NewLine +
                $"Accept using this invitation token (valid for {validForHours} hour(s)):" + Environment.NewLine + Environment.NewLine +
                invitationToken + Environment.NewLine + Environment.NewLine +
                $"Then register via POST api/workspaces/{workspaceId}/invite/<token>/accept with your details." + Environment.NewLine + Environment.NewLine +
                "If you did not expect this invitation, you can ignore this email.";

            var innerHtml =
                "<p style=\"margin:0 0 16px;font-size:15px;line-height:1.5;color:#374151;\">" +
                "You have been invited to join a workspace.</p>" +
                "<p style=\"margin:0 0 8px;font-size:13px;color:#6b7280;\">Workspace ID</p>" +
                "<p style=\"margin:0 0 16px;font-size:15px;font-weight:600;color:#111827;\">" + workspaceEncoded + "</p>" +
                "<p style=\"margin:0 0 8px;font-size:13px;color:#6b7280;\">Invitation token (expires in " + validForHours + " hour(s))</p>" +
                "<pre style=\"margin:0 0 20px;padding:14px 16px;background:#f3f4f6;border-radius:8px;border:1px solid #e5e7eb;" +
                "font-family:ui-monospace,Consolas,monospace;font-size:13px;line-height:1.45;color:#111827;white-space:pre-wrap;word-break:break-all;\">" +
                tokenEncoded + "</pre>" +
                "<p style=\"margin:0;font-size:14px;line-height:1.5;color:#6b7280;\">Use your API client to complete signup at " +
                "<code style=\"font-size:12px;background:#f3f4f6;padding:2px 6px;border-radius:4px;\">POST …/workspaces/{id}/invite/{token}/accept</code>. " +
                "If you did not expect this, ignore this email.</p>";

            return new EmailContent(
                $"Invitation to a {BrandName} workspace",
                plain,
                WrapLayout("Workspace invitation", innerHtml));
        }

        private static string WrapLayout(string headline, string innerHtml)
        {
            var h = WebUtility.HtmlEncode(headline);
            return "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                "<title>" + h + "</title></head>" +
                "<body style=\"margin:0;padding:0;background-color:#f4f4f5;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;\">" +
                "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background-color:#f4f4f5;padding:28px 16px;\">" +
                "<tr><td align=\"center\">" +
                "<table role=\"presentation\" width=\"560\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:560px;width:100%;background:#ffffff;" +
                "border-radius:10px;box-shadow:0 1px 3px rgba(15,23,42,.08);\">" +
                "<tr><td style=\"padding:28px 32px 8px;\">" +
                "<p style=\"margin:0 0 4px;font-size:13px;font-weight:600;letter-spacing:.04em;color:#2563eb;text-transform:uppercase;\">" + WebUtility.HtmlEncode(BrandName) + "</p>" +
                "<h1 style=\"margin:0 0 20px;font-size:22px;line-height:1.3;color:#111827;font-weight:600;\">" + h + "</h1>" +
                "</td></tr>" +
                "<tr><td style=\"padding:0 32px 28px;\">" + innerHtml + "</td></tr>" +
                "<tr><td style=\"padding:16px 32px 24px;border-top:1px solid #e5e7eb;font-size:12px;line-height:1.5;color:#9ca3af;\">" +
                "This message was sent by " + WebUtility.HtmlEncode(BrandName) + ". Please do not reply to this email." +
                "</td></tr></table></td></tr></table></body></html>";
        }
    }
}
