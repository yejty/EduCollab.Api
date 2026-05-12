using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Services.Users
{
    public interface IUserService
    {
        Task ChangePasswordAsync(string password, string newPassword, CancellationToken cancellationToken);
        Task ConfirmResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
        Task CreateAsync(User user, string password, string invitationToken, CancellationToken cancellationToken);
        Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken);
        Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken);
        Task InviteAsync(string email, CancellationToken cancellationToken);
        Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken);
        Task RegisterAsync(User user, string password, CancellationToken cancellationToken);
        Task ResetPasswordAsync(string email, CancellationToken cancellationToken);
        Task<User?> UpdateUserByIdAsync(User user, CancellationToken cancellationToken);
    }
}
