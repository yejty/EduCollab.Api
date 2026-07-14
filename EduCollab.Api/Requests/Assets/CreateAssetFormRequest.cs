namespace EduCollab.Api.Requests.Assets;

public sealed class CreateAssetFormRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Groups that receive access to this asset when it is created.
    /// Omit to keep the asset in your personal space.
    /// </summary>
    public List<int>? GroupIds { get; set; }

    public string? Description { get; set; }

    public IFormFile? File { get; set; }
}
