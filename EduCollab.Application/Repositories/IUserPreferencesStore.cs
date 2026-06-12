namespace EduCollab.Application.Repositories
{
    public interface IUserPreferencesStore
    {
        Task<string?> GetAsync(int userId, CancellationToken cancellationToken);
        Task SaveAsync(int userId, string json, CancellationToken cancellationToken);
    }
}
