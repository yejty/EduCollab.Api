namespace EduCollab.Application.Services.Sessions
{
    public sealed class InternalApiSettings
    {
        public const string SectionName = "InternalApi";

        /// <summary>
        /// Shared secret sent by Colyseus as <c>X-Api-Key</c> on room lifecycle webhooks.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}
