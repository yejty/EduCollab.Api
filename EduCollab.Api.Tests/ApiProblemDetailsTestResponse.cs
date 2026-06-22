namespace EduCollab.Api.Tests;

/// <summary>
/// Test deserialization shape for RFC 9457 problem responses emitted by the API.
/// </summary>
public sealed class ApiProblemDetailsTestResponse
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int? Status { get; set; }

    public string? Detail { get; set; }

    public string? Error { get; set; }

    public string? RequestId { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }
}
