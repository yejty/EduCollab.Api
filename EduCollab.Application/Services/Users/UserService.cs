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

        public Task CreateAsync(User user, string password, string invitationToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InviteAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));

            var userCredentialRecord = await _userRepository.GetCredentialByEmailAsync(email, cancellationToken);
            if (userCredentialRecord is null || string.IsNullOrEmpty(userCredentialRecord.PasswordHash))
                return null;

            var hashingUser = new PasswordHasherUser { Id = userCredentialRecord.Id.ToString() };
            var verification = _passwordHasher.VerifyHashedPassword(hashingUser, userCredentialRecord.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
                return null;

            return new User
            {
                Id = userCredentialRecord.Id,
                Email = userCredentialRecord.Email
            };
        } 

        public async Task RegisterAsync(User user, string password, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user);

            var existing = await _userRepository.GetCredentialByEmailAsync(user.Email, cancellationToken);
            if (existing is not null)
                throw new ArgumentException("A user with this email already exists.");

            var hashingUser = new PasswordHasherUser { Id = user.Email };
            var hash = _passwordHasher.HashPassword(hashingUser, password);
            user.Id = await _userRepository.InsertRegisteredUserAsync(
                user.FirstName,
                user.LastName,
                user.Email,
                hash,
                cancellationToken);
        }

        public Task ResetPasswordAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<User?> UpdateUserByIdAsync(User user, CancellationToken cancellationToken)
        {
            var userExists = await _userRepository.ExistsByIdAsync(user.Id, cancellationToken);
            if (!userExists)
            {
                return null;
            }
            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        public Task<User?> GetUserByIdAsync(int id, string token, CancellationToken cancellationToken)
        {
            return _userRepository.GetUserByIdAsync(id, cancellationToken);
        }

        public Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
