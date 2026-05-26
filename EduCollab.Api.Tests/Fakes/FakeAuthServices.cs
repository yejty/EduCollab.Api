using EduCollab.Api.Security;
using EduCollab.Application.Models;
using EduCollab.Application.Services.Auth;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeAccessTokenService : IAccessTokenService
{
    public Func<int, string, string>? CreateHandler { get; set; }

    public string Create(int userId, string email) =>
        CreateHandler?.Invoke(userId, email) ?? $"access-token-for-{userId}";
}

public sealed class FakeRefreshTokenService : IRefreshTokenService
{
    public Func<int, CancellationToken, Task<string>>? CreateAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task<RefreshSessionResult?>>? RefreshAsyncHandler { get; set; }

    public Task<string> CreateAsync(int userId, CancellationToken cancellationToken) =>
        CreateAsyncHandler?.Invoke(userId, cancellationToken) ?? Task.FromResult($"refresh-token-for-{userId}");

    public Task<RefreshSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        RefreshAsyncHandler?.Invoke(refreshToken, cancellationToken) ?? Task.FromResult<RefreshSessionResult?>(null);
}
