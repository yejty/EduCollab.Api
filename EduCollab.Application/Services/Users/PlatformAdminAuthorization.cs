using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Users
{
    public sealed class PlatformAdminAuthorization : IPlatformAdminAuthorization
    {
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _userRepository;

        public PlatformAdminAuthorization(ICurrentUser currentUser, IUserRepository userRepository)
        {
            _currentUser = currentUser;
            _userRepository = userRepository;
        }

        public async Task EnsureCurrentUserIsPlatformAdminAsync(CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");

            if (!await _userRepository.IsPlatformAdminAsync(userId, cancellationToken))
                throw new AccessDeniedException("Insufficient rights.");
        }
    }
}
