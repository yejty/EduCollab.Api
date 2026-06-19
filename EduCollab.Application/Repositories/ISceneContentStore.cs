namespace EduCollab.Application.Repositories
{
    public interface ISceneContentStore
    {
        Task<string?> GetAsync(int workspaceId, int sceneId, int versionNumber, CancellationToken cancellationToken);

        Task SaveAsync(int workspaceId, int sceneId, int versionNumber, string jsonContent, CancellationToken cancellationToken);

        Task CopyContentAsync(int workspaceId, int sceneId, int fromVersionNumber, int toVersionNumber, CancellationToken cancellationToken);

        Task DeleteAllVersionsAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
    }
}
