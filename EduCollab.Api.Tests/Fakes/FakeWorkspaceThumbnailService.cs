using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Workspaces;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeWorkspaceThumbnailService : IWorkspaceThumbnailService
{
    public Func<CancellationToken, Task<WorkspaceThumbnailContent?>>? GetCurrentWorkspaceThumbnailAsyncHandler { get; set; }
    public Func<string, Stream, CancellationToken, Task>? SaveCurrentWorkspaceThumbnailAsyncHandler { get; set; }
    public Func<CancellationToken, Task>? DeleteCurrentWorkspaceThumbnailAsyncHandler { get; set; }

    public Task<WorkspaceThumbnailContent?> GetCurrentWorkspaceThumbnailAsync(CancellationToken cancellationToken) =>
        GetCurrentWorkspaceThumbnailAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult<WorkspaceThumbnailContent?>(null);

    public Task SaveCurrentWorkspaceThumbnailAsync(string contentType, Stream content, CancellationToken cancellationToken) =>
        SaveCurrentWorkspaceThumbnailAsyncHandler?.Invoke(contentType, content, cancellationToken) ?? Task.CompletedTask;

    public Task DeleteCurrentWorkspaceThumbnailAsync(CancellationToken cancellationToken) =>
        DeleteCurrentWorkspaceThumbnailAsyncHandler?.Invoke(cancellationToken) ?? Task.CompletedTask;
}
