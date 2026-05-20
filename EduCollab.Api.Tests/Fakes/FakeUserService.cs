using EduCollab.Application.Models.Users;
using EduCollab.Application.Services.Users;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeUserService : IUserService
{
    public Func<string, string, CancellationToken, Task>? ChangePasswordAsyncHandler { get; set; }
    public Func<string, string, string, CancellationToken, Task>? ConfirmResetPasswordAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? DeleteUserByIdAsyncHandler { get; set; }
    public Func<CancellationToken, Task<User?>>? GetCurrentUserAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<User?>>? GetUserByIdAsyncHandler { get; set; }
    public Func<string, string, CancellationToken, Task<User?>>? LoginAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task>? RequestLoginCodeAsyncHandler { get; set; }
    public Func<string, string, CancellationToken, Task<LoginWithCodeResult>>? LoginWithCodeAsyncHandler { get; set; }
    public Func<User, string, CancellationToken, Task<bool>>? RegisterAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task>? ResendEmailConfirmationAsyncHandler { get; set; }
    public Func<string, CancellationToken, Task>? ResetPasswordAsyncHandler { get; set; }
    public Func<User, CancellationToken, Task<User?>>? UpdateUserByIdAsyncHandler { get; set; }
    public Func<string, string, CancellationToken, Task<User?>>? ConfirmEmailAsyncHandler { get; set; }

    public Task ChangePasswordAsync(string password, string newPassword, CancellationToken cancellationToken) =>
        ChangePasswordAsyncHandler?.Invoke(password, newPassword, cancellationToken) ?? Task.CompletedTask;

    public Task ConfirmResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken) =>
        ConfirmResetPasswordAsyncHandler?.Invoke(email, token, newPassword, cancellationToken) ?? Task.CompletedTask;

    public Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken) =>
        DeleteUserByIdAsyncHandler?.Invoke(id, cancellationToken) ?? Task.FromResult(true);

    public Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        GetCurrentUserAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult<User?>(null);

    public Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken) =>
        GetUserByIdAsyncHandler?.Invoke(id, cancellationToken) ?? Task.FromResult<User?>(null);

    public Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken) =>
        LoginAsyncHandler?.Invoke(email, password, cancellationToken) ?? Task.FromResult<User?>(null);

    public Task RequestLoginCodeAsync(string email, CancellationToken cancellationToken) =>
        RequestLoginCodeAsyncHandler?.Invoke(email, cancellationToken) ?? Task.CompletedTask;

    public Task<LoginWithCodeResult> LoginWithCodeAsync(string email, string code, CancellationToken cancellationToken) =>
        LoginWithCodeAsyncHandler?.Invoke(email, code, cancellationToken)
        ?? Task.FromResult(new LoginWithCodeResult());

    public Task<bool> RegisterAsync(User user, string password, CancellationToken cancellationToken) =>
        RegisterAsyncHandler?.Invoke(user, password, cancellationToken) ?? Task.FromResult(true);

    public Task ResendEmailConfirmationAsync(string email, CancellationToken cancellationToken) =>
        ResendEmailConfirmationAsyncHandler?.Invoke(email, cancellationToken) ?? Task.CompletedTask;

    public Task ResetPasswordAsync(string email, CancellationToken cancellationToken) =>
        ResetPasswordAsyncHandler?.Invoke(email, cancellationToken) ?? Task.CompletedTask;

    public Task<User?> UpdateUserByIdAsync(User user, CancellationToken cancellationToken) =>
        UpdateUserByIdAsyncHandler?.Invoke(user, cancellationToken) ?? Task.FromResult<User?>(user);

    public Task<User?> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken) =>
        ConfirmEmailAsyncHandler?.Invoke(email, token, cancellationToken) ?? Task.FromResult<User?>(null);
}
