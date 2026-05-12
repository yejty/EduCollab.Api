using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Repositories.Users;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<PasswordHasherUser> _passwordHasher;
        private readonly ICurrentUser _currentUser;
        private readonly IOptions<PasswordResetSettings> _passwordResetSettings;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<PasswordHasherUser> passwordHasher,
            ICurrentUser currentUser,
            IOptions<PasswordResetSettings> passwordResetSettings,
            IHostEnvironment hostEnvironment,
            IEmailSender emailSender,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
            _passwordResetSettings = passwordResetSettings;
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task ChangePasswordAsync(string password, string newPassword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));

            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentException($"'{nameof(newPassword)}' cannot be null or empty.", nameof(newPassword));

            var userId = _currentUser.UserId;
            if (userId is null)
                throw new UnauthorizedAccessException("Authentication is required for this operation.");

            var userCredRecord = await _userRepository.GetCredentialByIdAsync(userId.Value, cancellationToken);
            if (userCredRecord is null || string.IsNullOrEmpty(userCredRecord.PasswordHash))
                throw new InvalidOperationException("Password change is not available for this account.");

            var hashingUser = new PasswordHasherUser { Id = userCredRecord.Id.ToString() };
            if (_passwordHasher.VerifyHashedPassword(hashingUser, userCredRecord.PasswordHash, password) == PasswordVerificationResult.Failed)
                throw new ArgumentException("Current password is incorrect.");

            var newHash = _passwordHasher.HashPassword(hashingUser, newPassword);
            await _userRepository.UpdatePasswordHashAsync(userId.Value, newHash, cancellationToken);

            await NotifyPasswordChangedAsync(userCredRecord.Email, cancellationToken);
        }

        public async Task ConfirmResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException($"'{nameof(token)}' cannot be null or empty.", nameof(token));

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException($"'{nameof(newPassword)}' cannot be null or empty.", nameof(newPassword));

            var normalizedEmail = email.Trim();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(token.Trim());

            var userIdForHasher = await _userRepository.GetUserIdForActivePasswordResetTokenAsync(normalizedEmail, tokenHash, DateTimeOffset.UtcNow, cancellationToken);
            if (userIdForHasher is null)
                throw new ArgumentException("Invalid or expired password reset token.");

            var hashingUser = new PasswordHasherUser { Id = userIdForHasher.Value.ToString() };
            var newHash = _passwordHasher.HashPassword(hashingUser, newPassword);

            var now = DateTimeOffset.UtcNow;
            var userId = await _userRepository.CompletePasswordResetAsync(normalizedEmail, tokenHash, newHash, now, cancellationToken);
            if (userId is null)
                throw new ArgumentException("Invalid or expired password reset token.");

            await NotifyPasswordChangedAsync(normalizedEmail, cancellationToken);
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

        public async Task<bool> RegisterAsync(User user, string password, CancellationToken cancellationToken)
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

            if (user.Id <= 0)
                return false;

            var hashingUserWithId = new PasswordHasherUser { Id = user.Id.ToString() };
            var hashForLogin = _passwordHasher.HashPassword(hashingUserWithId, password);
            await _userRepository.UpdatePasswordHashAsync(user.Id, hashForLogin, cancellationToken);
            return true;
        }

        public async Task ResetPasswordAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            var normalizedEmail = email.Trim();
            var cred = await _userRepository.GetCredentialByEmailAsync(normalizedEmail, cancellationToken);
            if (cred is null || string.IsNullOrEmpty(cred.PasswordHash))
                return;

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddHours(_passwordResetSettings.Value.TokenExpirationHours);

            await _userRepository.RevokeActivePasswordResetTokensForUserAsync(cred.Id, now, cancellationToken);

            var plainToken = RefreshTokenGenerator.Create();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(plainToken);
            await _userRepository.InsertPasswordResetTokenAsync(cred.Id, tokenHash, expiresAt, now, cancellationToken);

            var hours = _passwordResetSettings.Value.TokenExpirationHours;
            var resetMail = EduCollabEmailTemplates.PasswordResetRequest(plainToken, hours);
            await _emailSender.SendAsync(normalizedEmail, resetMail, cancellationToken);

            if (_hostEnvironment.IsDevelopment() && _passwordResetSettings.Value.LogPlaintextTokenInDevelopment)
                _logger.LogInformation("Password reset token for {Email}: {Token}", normalizedEmail, plainToken);
        }

        public async Task<User?> UpdateUserByIdAsync(User user, CancellationToken cancellationToken)
        {
            EnsureCallerOwnsUser(user.Id);

            var userExists = await _userRepository.ExistsByIdAsync(user.Id, cancellationToken);
            if (!userExists)
            {
                return null;
            }

            var updated = await _userRepository.UpdateAsync(user, cancellationToken);
            if (!updated)
                return null;

            await NotifyProfileUpdatedAsync(user, cancellationToken);
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken)
        {
            EnsureCallerOwnsUser(id);
            return await _userRepository.GetUserByIdAsync(id, cancellationToken);
        }

        public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (userId is null)
                return null;

            return await _userRepository.GetUserByIdAsync(userId.Value, cancellationToken);
        }

        private void EnsureCallerOwnsUser(int userId)
        {
            var callerId = _currentUser.UserId;
            if (callerId is null)
                throw new UnauthorizedAccessException("Authentication is required for this operation.");

            if (callerId.Value != userId)
                throw new AccessDeniedException("You can only access or change your own user record.");
        }

        private Task NotifyProfileUpdatedAsync(User user, CancellationToken cancellationToken)
        {
            var mail = EduCollabEmailTemplates.ProfileUpdated(user);
            return _emailSender.SendAsync(user.Email, mail, cancellationToken);
        }

        private Task NotifyPasswordChangedAsync(string email, CancellationToken cancellationToken)
        {
            var mail = EduCollabEmailTemplates.PasswordChanged();
            return _emailSender.SendAsync(email, mail, cancellationToken);
        }

        public async Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken)
        {
            EnsureCallerOwnsUser(id);
            return await _userRepository.DeleteUserByIdAsync(id, cancellationToken);
        }
    }
}
