namespace EduCollab.Application.Repositories
{
    public sealed record WorkspaceThumbnailContent(string ContentType, byte[] Data);

    public interface IWorkspaceThumbnailStore
    {
        Task<WorkspaceThumbnailContent?> GetAsync(int workspaceId, CancellationToken cancellationToken);

        Task SaveAsync(int workspaceId, string contentType, Stream content, CancellationToken cancellationToken);

        Task DeleteAsync(int workspaceId, CancellationToken cancellationToken);
    }
}
