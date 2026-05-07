using EduCollab.Application.Models.Users;
using EduCollab.Application.Repositories.Users;
using Microsoft.AspNetCore.Identity;

namespace EduCollab.Application.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<PasswordHasherUser> _passwordHasher;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<PasswordHasherUser> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

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

        public async Task<AuthenticatedUser?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            var record = await _userRepository.GetCredentialByEmailAsync(email, cancellationToken);
            if (record is null || string.IsNullOrEmpty(record.PasswordHash))
                return null;

            var hashingUser = new PasswordHasherUser { Id = record.Id.ToString() };
            var verification = _passwordHasher.VerifyHashedPassword(hashingUser, record.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
                return null;

            return new AuthenticatedUser(record.Id, record.Email);
        }

        public async Task RegisterAsync(string firstName, string lastName, string email, string password, CancellationToken cancellationToken)
        {
            var existing = await _userRepository.GetCredentialByEmailAsync(email, cancellationToken);
            if (existing is not null)
                throw new ArgumentException("A user with this email already exists.");

            var hashingUser = new PasswordHasherUser { Id = email };
            var hash = _passwordHasher.HashPassword(hashingUser, password);
            await _userRepository.InsertRegisteredUserAsync(firstName, lastName, email, hash, cancellationToken);
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
