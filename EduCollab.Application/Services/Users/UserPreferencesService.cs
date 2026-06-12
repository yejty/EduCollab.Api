using System.Text.Json;
using EduCollab.Application.Identity;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Users
{
    public sealed class UserPreferencesService : IUserPreferencesService
    {
        private readonly IUserPreferencesStore _store;
        private readonly ICurrentUser _currentUser;

        public UserPreferencesService(IUserPreferencesStore store, ICurrentUser currentUser)
        {
            _store = store;
            _currentUser = currentUser;
        }

        public Task<string?> GetCurrentUserPreferencesAsync(CancellationToken cancellationToken)
        {
            return _store.GetAsync(RequireCurrentUserId(), cancellationToken);
        }

        public async Task<string> SaveCurrentUserPreferencesAsync(string json, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Preferences payload is required.", nameof(json));

            // Validate JSON syntax while keeping the backend agnostic to its schema.
            using var _ = JsonDocument.Parse(json);

            var userId = RequireCurrentUserId();
            await _store.SaveAsync(userId, json, cancellationToken);
            return json;
        }

        private int RequireCurrentUserId()
        {
            return _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }
    }
}
