namespace EduCollab.Application.Services.Notifications
{
    /// <summary>
    /// Email payload: always includes plain text; optional HTML for multipart/alternative.
    /// </summary>
    public sealed record EmailContent(string Subject, string PlainText, string? HtmlBody = null);
}
