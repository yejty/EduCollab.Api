using EduCollab.Application.Models;

namespace EduCollab.Application.Services.Auth
{
    public interface IRefreshTokenService
    {
        Task<string> CreateAsync(int userId, CancellationToken cancellationToken);

        Task<RefreshSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
