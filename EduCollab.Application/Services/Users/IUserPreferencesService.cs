namespace EduCollab.Application.Services.Users
{
    public interface IUserPreferencesService
    {
        Task<string?> GetCurrentUserPreferencesAsync(CancellationToken cancellationToken);
        Task<string> SaveCurrentUserPreferencesAsync(string json, CancellationToken cancellationToken);
    }
}
