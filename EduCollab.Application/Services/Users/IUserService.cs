using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Services.Users
{
    public interface IUserService
    {
        Task ChangePasswordAsync(string password, string newPassword, CancellationToken cancellationToken);
        Task ConfirmResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
        Task CreateAsync(string firstName, string lastName, string email, string password, string invitationToken, CancellationToken cancellationToken);
        Task GetCurrentUserAsync(CancellationToken cancellationToken);
        Task GetUserByIdAsync(int id, string token, CancellationToken cancellationToken);
        Task InviteAsync(string email, CancellationToken cancellationToken);
        Task<AuthenticatedUser?> LoginAsync(string email, string password, CancellationToken cancellationToken);
        Task<string> CreateRefreshTokenAsync(int userId, CancellationToken cancellationToken);
        Task<RefreshSessionResult?> RefreshSessionAsync(string refreshToken, CancellationToken cancellationToken);
        Task RegisterAsync(string firstName, string lastName, string email, string password, CancellationToken cancellationToken);
        Task ResetPasswordAsync(string email, CancellationToken cancellationToken);
        Task UpdateUserByIdAsync(int id, string token, CancellationToken cancellationToken);
    }
}
