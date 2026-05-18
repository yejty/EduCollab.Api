using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Services.Users
{
    public interface IUserService
    {
        Task ChangePasswordAsync(string password, string newPassword, CancellationToken cancellationToken);
        Task ConfirmResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
        Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken);
        Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken);
        Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken);   
        Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken);
        Task RequestLoginCodeAsync(string email, CancellationToken cancellationToken);
        Task<LoginWithCodeResult> LoginWithCodeAsync(string email, string code, CancellationToken cancellationToken);
        Task<bool> RegisterAsync(User user, string password, CancellationToken cancellationToken);
        Task ResetPasswordAsync(string email, CancellationToken cancellationToken);
        Task<User?> UpdateUserByIdAsync(User user, CancellationToken cancellationToken);

        Task<User?> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken);
    }
}
