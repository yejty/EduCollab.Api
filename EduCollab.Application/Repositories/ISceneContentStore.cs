namespace EduCollab.Application.Repositories
{
    public interface ISceneContentStore
    {
        Task<string?> GetAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);

        Task SaveAsync(int workspaceId, int sceneId, string jsonContent, CancellationToken cancellationToken);

        Task DeleteAsync(int workspaceId, int sceneId, CancellationToken cancellationToken);
    }
}
