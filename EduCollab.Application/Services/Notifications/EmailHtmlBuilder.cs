using System.Net;
using T = EduCollab.Application.Services.Notifications.EmailDesignTokens;

namespace EduCollab.Application.Services.Notifications
{
    /// <summary>
    /// Inline HTML fragments for transactional emails (table layout, token-based styles).
    /// </summary>
    internal static class EmailHtmlBuilder
    {
        public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        public static string WrapDocument(string brandName, string headline, string innerHtml)
        {
            var h = Encode(headline);
            var brand = Encode(brandName);

            return "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
                "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                "<meta name=\"color-scheme\" content=\"dark\">" +
                "<meta name=\"supported-color-schemes\" content=\"dark\">" +
                "<title>" + h + "</title></head>" +
                "<body style=\"margin:0;padding:0;background-color:" + T.AppBackground + ";font-family:" + T.FontSans +
                ";font-weight:300;font-size:" + T.FontSizeSm + "px;line-height:1.45;color:" + T.TextPrimary + ";\">" +
                "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background-color:" +
                T.AppBackground + ";padding:24px 16px;\">" +
                "<tr><td align=\"center\">" +
                "<table role=\"presentation\" width=\"" + T.ContentWidth + "\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:" +
                T.ContentWidth + "px;width:100%;background:" + T.PanelBackground + ";border:1px solid " + T.Border +
                ";border-radius:" + T.RadiusMd + "px;\">" +
                "<tr><td style=\"padding:24px 28px 8px;border-bottom:1px solid " + T.Border + ";\">" +
                "<p style=\"margin:0 0 6px;font-size:" + T.FontSizeSm + "px;font-weight:600;letter-spacing:.06em;color:" +
                T.Accent + ";text-transform:uppercase;\">" + brand + "</p>" +
                "<h1 style=\"margin:0;font-size:" + T.FontSizeLg + "px;line-height:1.35;color:" + T.TextPrimary +
                ";font-weight:600;\">" + h + "</h1>" +
                "</td></tr>" +
                "<tr><td style=\"padding:20px 28px 24px;\">" + innerHtml + "</td></tr>" +
                "<tr><td style=\"padding:14px 28px 20px;border-top:1px solid " + T.Border + ";font-size:" + T.FontSizeXs +
                "px;line-height:1.5;color:" + T.TextMuted + ";\">" +
                "This message was sent by " + brand + ". Please do not reply to this email." +
                "</td></tr></table></td></tr></table></body></html>";
        }

        public static string Paragraph(string htmlContent) =>
            "<p style=\"margin:0 0 12px;font-size:" + T.FontSizeMd + "px;line-height:1.5;color:" +
            T.TextPrimary + ";\">" + htmlContent + "</p>";

        public static string Muted(string htmlContent) =>
            "<p style=\"margin:0 0 16px;font-size:" + T.FontSizeSm + "px;line-height:1.45;color:" +
            T.TextSecondary + ";\">" + htmlContent + "</p>";

        public static string FinePrint(string htmlContent) =>
            "<p style=\"margin:0;font-size:" + T.FontSizeSm + "px;line-height:1.5;color:" +
            T.TextMuted + ";\">" + htmlContent + "</p>";

        public static string Label(string text) =>
            "<p style=\"margin:0 0 6px;font-size:" + T.FontSizeSm + "px;color:" +
            T.TextSecondary + ";\">" + Encode(text) + "</p>";

        public static string PrimaryButton(string url, string label) =>
            "<p style=\"margin:0 0 20px;\"><a href=\"" + Encode(url) + "\" style=\"display:inline-block;" +
            "min-height:28px;line-height:28px;padding:0 18px;background:" + T.Accent + ";color:" + T.OnAccent +
            ";text-decoration:none;border-radius:" + T.RadiusXs + "px;font-size:" + T.FontSizeMd +
            "px;font-weight:500;box-sizing:border-box;\">" + Encode(label) + "</a></p>";

        public static string ActionList(IEnumerable<NotificationAction> actions)
        {
            var rendered = actions
                .Where(action => !string.IsNullOrWhiteSpace(action.Url) && !string.IsNullOrWhiteSpace(action.Label))
                .Select(ActionButton)
                .ToArray();

            if (rendered.Length == 0)
            {
                return string.Empty;
            }

            return "<p style=\"margin:0 0 20px;\">" + string.Concat(rendered) + "</p>";
        }

        private static string ActionButton(NotificationAction action)
        {
            var (background, color, border) = action.Style switch
            {
                NotificationActionStyle.Danger => (T.Danger, T.OnAccent, T.Danger),
                NotificationActionStyle.Secondary => (T.PanelAltBackground, T.TextPrimary, T.BorderStrong),
                _ => (T.Accent, T.OnAccent, T.Accent)
            };

            return "<a href=\"" + Encode(action.Url) + "\" style=\"display:inline-block;min-height:28px;line-height:28px;" +
                "padding:0 18px;margin:0 4px 4px 0;background:" + background + ";color:" + color +
                ";border:1px solid " + border + ";text-decoration:none;border-radius:" + T.RadiusXs +
                "px;font-size:" + T.FontSizeMd + "px;font-weight:500;box-sizing:border-box;\">" +
                Encode(action.Label) + "</a>";
        }

        public static string UrlFallback(string url) =>
            Label("Or paste this URL into your browser:") +
            "<p style=\"margin:0 0 16px;font-size:" + T.FontSizeSm + "px;line-height:1.45;color:" + T.TextSecondary +
            ";word-break:break-all;\">" + Encode(url) + "</p>";

        public static string CodeBlock(string content, bool large = false)
        {
            var fontSize = large ? T.FontSizeXl : T.FontSizeMd;
            var extra = large ? "font-weight:600;letter-spacing:4px;text-align:center;" : "word-break:break-all;";
            return "<pre style=\"margin:0 0 16px;padding:12px 14px;background:" + T.PanelAltBackground +
                ";border-radius:" + T.RadiusXs + "px;border:1px solid " + T.Border + ";font-family:" + T.FontMono +
                ";font-size:" + fontSize + "px;line-height:1.45;color:" + T.TextPrimary + ";white-space:pre-wrap;" +
                extra + "\">" + Encode(content) + "</pre>";
        }

        public static string WarningCallout(string htmlContent) =>
            "<p style=\"margin:0;font-size:" + T.FontSizeMd + "px;line-height:1.5;color:" + T.TextPrimary +
            ";background:" + T.PanelAltBackground + ";padding:12px 14px;border-radius:" + T.RadiusXs + "px;" +
            "border:1px solid " + T.Border + ";border-left:3px solid " + T.Danger + ";\">" + htmlContent + "</p>";
    }
}
