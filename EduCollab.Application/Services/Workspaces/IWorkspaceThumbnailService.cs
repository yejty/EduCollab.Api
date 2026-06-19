using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceThumbnailService
    {
        Task<WorkspaceThumbnailContent?> GetCurrentWorkspaceThumbnailAsync(CancellationToken cancellationToken);

        Task SaveCurrentWorkspaceThumbnailAsync(string contentType, Stream content, CancellationToken cancellationToken);

        Task DeleteCurrentWorkspaceThumbnailAsync(CancellationToken cancellationToken);
    }
}
