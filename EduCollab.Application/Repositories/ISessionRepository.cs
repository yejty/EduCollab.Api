using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface ISessionRepository
    {
        Task<int> CreateSessionAsync(LiveSession session, IReadOnlyList<int> groupIds, IReadOnlyList<int> userIds, CancellationToken cancellationToken);

        Task<LiveSession?> GetSessionByIdAsync(int workspaceId, int sessionId, CancellationToken cancellationToken);

        Task<LiveSession?> GetSessionByIdAsync(int sessionId, CancellationToken cancellationToken);

        Task<List<LiveSession>> ListSessionsAsync(
            int workspaceId,
            string? status,
            int? sharedWithUserId,
            IReadOnlyCollection<int> accessibleGroupIds,
            CancellationToken cancellationToken);

        Task<bool> EndSessionAsync(int workspaceId, int sessionId, CancellationToken cancellationToken);

        Task<bool> MarkRoomStartedAsync(int sessionId, CancellationToken cancellationToken);

        Task<bool> MarkRoomEndedAsync(int sessionId, CancellationToken cancellationToken);

        Task ReplaceSessionSharesAsync(
            int sessionId,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<int> userIds,
            CancellationToken cancellationToken);

        Task AddParticipantAsync(
            int sessionId,
            int? userId,
            string? guestId,
            string displayName,
            CancellationToken cancellationToken);

        Task<SessionGuestLink> UpsertGuestLinkAsync(SessionGuestLink link, CancellationToken cancellationToken);

        Task<SessionGuestLink?> GetActiveGuestLinkBySessionIdAsync(int sessionId, CancellationToken cancellationToken);

        Task<SessionGuestLink?> GetActiveGuestLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

        Task<bool> TryIncrementGuestLinkUseAsync(int guestLinkId, CancellationToken cancellationToken);

        Task<int> EndAbandonedSessionsAsync(DateTime olderThanUtc, CancellationToken cancellationToken);
    }
}
