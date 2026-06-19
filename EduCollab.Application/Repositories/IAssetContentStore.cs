namespace EduCollab.Application.Repositories
{
    public sealed record AssetContent(string ContentType, byte[] Data);

    public interface IAssetContentStore
    {
        Task<AssetContent?> GetAsync(int workspaceId, int assetId, int versionNumber, CancellationToken cancellationToken);

        Task SaveAsync(int workspaceId, int assetId, int versionNumber, string contentType, string? fileName, Stream content, CancellationToken cancellationToken);

        Task CopyContentAsync(int workspaceId, int assetId, int fromVersionNumber, int toVersionNumber, CancellationToken cancellationToken);

        Task DeleteAllVersionsAsync(int workspaceId, int assetId, CancellationToken cancellationToken);
    }
}
