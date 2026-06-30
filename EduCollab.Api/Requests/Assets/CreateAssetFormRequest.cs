namespace EduCollab.Api.Requests.Assets;

public sealed class CreateAssetFormRequest
{
    public string Name { get; set; } = string.Empty;

    public int GroupId { get; set; }

    public string? Description { get; set; }

    public IFormFile? File { get; set; }
}
