namespace EduCollab.Contracts.Responses
{
    /// <summary>
    /// API and dependency health status.
    /// </summary>
    public sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;

        public Dictionary<string, string> Checks { get; set; } = new(StringComparer.Ordinal);
    }
}
