using EduCollab.Application.Services.Users;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeUserPreferencesService : IUserPreferencesService
{
    public Func<CancellationToken, Task<string?>>? GetCurrentUserPreferencesAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task<string>>? SaveCurrentUserPreferencesAsyncHandler { get; set; }

    public Task<string?> GetCurrentUserPreferencesAsync(CancellationToken cancellationToken) =>
        GetCurrentUserPreferencesAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult<string?>("{}");

    public Task<string> SaveCurrentUserPreferencesAsync(string json, CancellationToken cancellationToken) =>
        SaveCurrentUserPreferencesAsyncHandler?.Invoke(json, cancellationToken) ?? Task.FromResult(json);
}
