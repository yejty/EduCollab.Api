using EduCollab.Application.Auth;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Repositories.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<PasswordHasherUser> _passwordHasher;
        private readonly IOptions<RefreshTokenSettings> _refreshTokenSettings;

        public UserService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher<PasswordHasherUser> passwordHasher,
            IOptions<RefreshTokenSettings> refreshTokenSettings)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _refreshTokenSettings = refreshTokenSettings;
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

        public async Task<string> CreateRefreshTokenAsync(int userId, CancellationToken cancellationToken)
        {
            var (plaintext, hash) = RefreshTokenGenerator.Create();
            var expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenSettings.Value.RefreshTokenExpirationDays);
            await _refreshTokenRepository.InsertAsync(userId, hash, expires, cancellationToken);
            return plaintext;
        }

        public async Task<RefreshSessionResult?> RefreshSessionAsync(string refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var hash = RefreshTokenGenerator.HashPlaintext(refreshToken.Trim());
            var stored = await _refreshTokenRepository.GetActiveByHashAsync(hash, cancellationToken);
            if (stored is null)
                return null;

            await _refreshTokenRepository.RevokeByIdAsync(stored.Id, DateTimeOffset.UtcNow, cancellationToken);

            var (newPlaintext, newHash) = RefreshTokenGenerator.Create();
            var expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenSettings.Value.RefreshTokenExpirationDays);
            await _refreshTokenRepository.InsertAsync(stored.UserId, newHash, expires, cancellationToken);

            var userRecord = await _userRepository.GetCredentialByIdAsync(stored.UserId, cancellationToken);
            if (userRecord is null)
                return null;

            return new RefreshSessionResult
            {
                User = new AuthenticatedUser(userRecord.Id, userRecord.Email),
                RefreshToken = newPlaintext,
            };
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
