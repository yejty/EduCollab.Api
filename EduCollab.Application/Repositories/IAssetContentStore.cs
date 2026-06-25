namespace EduCollab.Application.Repositories
{
    public sealed record AssetContent(string ContentType, byte[] Data);

    public interface IAssetContentStore
    {
        Task<AssetContent?> GetAsync(int workspaceId, int assetId, CancellationToken cancellationToken);

        Task SaveAsync(int workspaceId, int assetId, string contentType, string? fileName, Stream content, CancellationToken cancellationToken);

        Task DeleteAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
    }
}
