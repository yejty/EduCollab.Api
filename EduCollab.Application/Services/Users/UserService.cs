
namespace EduCollab.Application.Services.Users
{
    public class UserService : IUserService
    {
        public Task ChangePasswordAsync(string password, string newPassword, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ConfirmResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync(string firstName, string lastName, string email, string password, string invitationToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task GetUserByIdAsync(int id, string token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InviteAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RegisterAsync(string firstName, string lastName, string email, string password, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ResetPasswordAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateUserByIdAsync(int id, string token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
