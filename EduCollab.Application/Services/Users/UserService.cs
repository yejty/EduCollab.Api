using System;
using System.Globalization;
using System.Security.Cryptography;
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
        private readonly IOptions<EmailConfirmationSettings> e;
        private readonly IOptions<LoginCodeSettings> _loginCodeSettings;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<PasswordHasherUser> passwordHasher,
            ICurrentUser currentUser,
            IOptions<PasswordResetSettings> passwordResetSettings,
            IOptions<EmailConfirmationSettings> emailConfirmationSettings,
            IOptions<LoginCodeSettings> loginCodeSettings,
            IHostEnvironment hostEnvironment,
            INotificationService notificationService,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
            _passwordResetSettings = passwordResetSettings;
            e = emailConfirmationSettings;
            _loginCodeSettings = loginCodeSettings;
            _hostEnvironment = hostEnvironment;
            _notificationService = notificationService;
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

            if (!userCredentialRecord.EmailConfirmedAtUtc.HasValue)
                return null;

            return new User
            {
                Id = userCredentialRecord.Id,
                Email = userCredentialRecord.Email
            };
        }

        public async Task RequestLoginCodeAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            var normalizedEmail = email.Trim();
            var cred = await _userRepository.GetCredentialByEmailAsync(normalizedEmail, cancellationToken);
            if (cred is null || !cred.EmailConfirmedAtUtc.HasValue)
                return;

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(_loginCodeSettings.Value.CodeExpirationMinutes);

            await _userRepository.RevokeActiveLoginCodesForUserAsync(cred.Id, now, cancellationToken);

            var plainCode = CreateLoginCode();
            var codeHash = RefreshTokenGenerator.HashPlaintext(plainCode);
            await _userRepository.InsertLoginCodeAsync(cred.Id, codeHash, expiresAt, now, cancellationToken);

            var mail = EduCollabEmailTemplates.LoginCode(plainCode, _loginCodeSettings.Value.CodeExpirationMinutes);
            await _notificationService.SendAsync(
                NotificationMessage.Create(normalizedEmail, NotificationType.LoginCode, mail),
                cancellationToken);

            if (_hostEnvironment.IsDevelopment() && _loginCodeSettings.Value.LogPlaintextCodeInDevelopment)
                _logger.LogInformation("Login code for {Email}: {Code}", normalizedEmail, plainCode);
        }

        public async Task<LoginWithCodeResult> LoginWithCodeAsync(string email, string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException($"'{nameof(code)}' cannot be null or empty.", nameof(code));

            var normalizedEmail = email.Trim();
            var codeHash = RefreshTokenGenerator.HashPlaintext(code.Trim());
            var result = await _userRepository.ConsumeLoginCodeAsync(
                normalizedEmail,
                codeHash,
                DateTimeOffset.UtcNow,
                _loginCodeSettings.Value.MaxAttempts,
                cancellationToken);
            if (result.UserId is null)
            {
                return new LoginWithCodeResult
                {
                    IsLocked = result.IsLocked,
                    RemainingAttempts = result.RemainingAttempts
                };
            }

            var user = await _userRepository.GetUserByIdAsync(result.UserId.Value, cancellationToken);
            return new LoginWithCodeResult
            {
                User = user
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
                null,
                cancellationToken);

            if (user.Id <= 0)
                return false;

            var hashingUserWithId = new PasswordHasherUser { Id = user.Id.ToString() };
            var hashForLogin = _passwordHasher.HashPassword(hashingUserWithId, password);
            await _userRepository.UpdatePasswordHashAsync(user.Id, hashForLogin, cancellationToken);

            await SendEmailConfirmationAsync(user.Id, user.Email.Trim(), cancellationToken);

            return true;
        }

        public async Task ResendEmailConfirmationAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            var normalizedEmail = email.Trim();
            var existing = await _userRepository.GetCredentialByEmailAsync(normalizedEmail, cancellationToken);
            if (existing is null || existing.EmailConfirmedAtUtc.HasValue)
                return;

            await SendEmailConfirmationAsync(existing.Id, existing.Email, cancellationToken);
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
            var expiresAt = now.AddMinutes(_passwordResetSettings.Value.TokenExpirationMinutes);

            await _userRepository.RevokeActivePasswordResetTokensForUserAsync(cred.Id, now, cancellationToken);

            var plainToken = RefreshTokenGenerator.Create();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(plainToken);
            await _userRepository.InsertPasswordResetTokenAsync(cred.Id, tokenHash, expiresAt, now, cancellationToken);

            var minutes = _passwordResetSettings.Value.TokenExpirationMinutes;
            var resetMail = EduCollabEmailTemplates.PasswordResetRequest(plainToken, minutes);
            await _notificationService.SendAsync(
                NotificationMessage.Create(normalizedEmail, NotificationType.PasswordReset, resetMail),
                cancellationToken);

            if (_hostEnvironment.IsDevelopment() && _passwordResetSettings.Value.LogPlaintextTokenInDevelopment)
                _logger.LogInformation("Password reset token for {Email}: {Token}", normalizedEmail, plainToken);
        }

        public async Task<User?> UpdateUserByIdAsync(User user, CancellationToken cancellationToken)
        {
            EnsureCallerOwnsUser(user.Id);

            var existing = await _userRepository.GetUserByIdAsync(user.Id, cancellationToken);
            if (existing is null)
                return null;

            user.Email = existing.Email;

            var updated = await _userRepository.UpdateAsync(user, cancellationToken);
            if (!updated)
                return null;

            await NotifyProfileUpdatedAsync(user, cancellationToken);
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken)
        {
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
            return _notificationService.SendAsync(
                NotificationMessage.Create(user.Email, NotificationType.ProfileUpdated, mail),
                cancellationToken);
        }

        private Task NotifyPasswordChangedAsync(string email, CancellationToken cancellationToken)
        {
            var mail = EduCollabEmailTemplates.PasswordChanged();
            return _notificationService.SendAsync(
                NotificationMessage.Create(email, NotificationType.PasswordChanged, mail),
                cancellationToken);
        }

        public async Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken)
        {
            EnsureCallerOwnsUser(id);
            return await _userRepository.DeleteUserByIdAsync(id, cancellationToken);
        }

        public async Task<User?> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException($"'{nameof(token)}' cannot be null or empty.", nameof(token));

            var normalizedEmail = email.Trim();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(token.Trim());
            var now = DateTimeOffset.UtcNow;

            var userId = await _userRepository.ConfirmEmailAsync(normalizedEmail, tokenHash, now, cancellationToken);
            if (userId is null)
                return null;

            return await _userRepository.GetUserByIdAsync(userId.Value, cancellationToken);
        }

        private async Task SendEmailConfirmationAsync(int userId, string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim();
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddHours(e.Value.TokenExpirationHours);

            await _userRepository.RevokeActiveEmailConfirmationTokensForUserAsync(userId, now, cancellationToken);

            var plainToken = RefreshTokenGenerator.Create();
            var tokenHash = RefreshTokenGenerator.HashPlaintext(plainToken);
            await _userRepository.InsertEmailConfirmationTokenAsync(userId, tokenHash, expiresAt, now, cancellationToken);

            var hours = e.Value.TokenExpirationHours;
            var baseUrl = e.Value.FrontendConfirmUrl?.Trim().TrimEnd('/') ?? string.Empty;
            string? confirmUrl = null;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                confirmUrl =
                    $"{baseUrl}?email={Uri.EscapeDataString(normalizedEmail)}&token={Uri.EscapeDataString(plainToken)}";
            }

            var confirmMail = EduCollabEmailTemplates.EmailConfirmation(confirmUrl ?? string.Empty, plainToken, hours);
            await _notificationService.SendAsync(
                NotificationMessage.Create(
                    normalizedEmail,
                    NotificationType.EmailConfirmation,
                    confirmMail,
                    actions: string.IsNullOrWhiteSpace(confirmUrl)
                        ? null
                        : new[] { new NotificationAction("Confirm email", confirmUrl) }),
                cancellationToken);

            if (_hostEnvironment.IsDevelopment() && e.Value.LogPlaintextTokenInDevelopment)
                _logger.LogInformation("Email confirmation token for {Email}: {Token}", normalizedEmail, plainToken);
        }

        private static string CreateLoginCode()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return value.ToString("D6", CultureInfo.InvariantCulture);
        }
    }
}
