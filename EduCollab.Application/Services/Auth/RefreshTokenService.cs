using EduCollab.Application.Models.Users;
using EduCollab.Application.Repositories.RefreshToken;
using EduCollab.Application.Repositories.Users;
using Microsoft.Extensions.Options;

namespace EduCollab.Application.Services.Auth
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOptions<RefreshTokenSettings> _refreshTokenSettings;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IOptions<RefreshTokenSettings> refreshTokenSettings)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _refreshTokenSettings = refreshTokenSettings;
        }

        public async Task<string> CreateAsync(int userId, CancellationToken cancellationToken)
        {
            var plainText = RefreshTokenGenerator.Create();
            var hash = RefreshTokenGenerator.HashPlaintext(plainText);
            var createdAt = DateTimeOffset.UtcNow;
            var expiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenSettings.Value.RefreshTokenExpirationDays);
            await _refreshTokenRepository.InsertAsync(userId, hash, expiresAt, createdAt, cancellationToken);
            return plainText;
        }

        public async Task<RefreshSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var hash = RefreshTokenGenerator.HashPlaintext(refreshToken.Trim());
            var stored = await _refreshTokenRepository.GetActiveByHashAsync(hash, cancellationToken);
            if (stored is null)
                return null;

            await _refreshTokenRepository.RevokeByIdAsync(stored.Id, DateTimeOffset.UtcNow, cancellationToken);

            var newRefreshToken = await CreateAsync(stored.UserId, cancellationToken);
            var userRecord = await _userRepository.GetCredentialByIdAsync(stored.UserId, cancellationToken);
            if (userRecord is null)
                return null;

            return new RefreshSessionResult
            {
                User = new AuthenticatedUser(userRecord.Id, userRecord.Email),
                RefreshToken = newRefreshToken,
            };
        }
    }
}
