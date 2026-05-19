namespace EduCollab.Application.Services.Notifications
{
    /// <summary>
    /// Email-safe color and typography values aligned with
    /// <c>Helpers/design-tokens.md</c> and <c>Helpers/index.css</c> (@theme / .dark).
    /// </summary>
    internal static class EmailDesignTokens
    {
        public const string AppBackground = "#1e1e1e";
        public const string PanelBackground = "#252526";
        public const string PanelAltBackground = "#2d2d30";
        public const string RowHover = "#2a2d2e";
        public const string Border = "#3f3f46";
        public const string BorderStrong = "#555555";

        public const string TextPrimary = "#dddddd";
        public const string TextSecondary = "#9b9b9b";
        public const string TextMuted = "#6b6b6b";

        public const string Accent = "#00ad09";
        public const string AccentHover = "#14c417";
        public const string OnAccent = "#ffffff";

        public const string Danger = "#d54545";
        public const string DangerHover = "#e25555";

        public const string FontSans =
            "Inter,'Segoe UI',system-ui,-apple-system,Roboto,Helvetica,Arial,sans-serif";

        public const string FontMono =
            "'JetBrains Mono',ui-monospace,Menlo,Consolas,monospace";

        public const int RadiusXs = 2;
        public const int RadiusSm = 4;
        public const int RadiusMd = 6;

        public const int FontSizeXs = 10;
        public const int FontSizeSm = 11;
        public const int FontSizeMd = 12;
        public const int FontSizeLg = 14;
        public const int FontSizeXl = 16;

        public const int ContentWidth = 560;
    }
}
