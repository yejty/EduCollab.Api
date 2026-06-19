namespace EduCollab.Application.Services.Notifications
{
    /// <summary>
    /// Minimal HTML pages returned when a user follows a one-click email action link.
    /// </summary>
    public static class EmailActionPages
    {
        private const string BrandName = "EduCollab";

        public static string Success(string headline, string message) =>
            EmailHtmlBuilder.WrapDocument(BrandName, headline,
                EmailHtmlBuilder.Paragraph(EmailHtmlBuilder.Encode(message)));

        public static string Error(string headline, string message) =>
            EmailHtmlBuilder.WrapDocument(BrandName, headline,
                EmailHtmlBuilder.WarningCallout(EmailHtmlBuilder.Encode(message)));
    }
}
