using EduCollab.Application.Models;
using EduCollab.Application.Repositories;

namespace EduCollab.Application.Services.Sessions
{
    public interface ISessionService
    {
        Task<LiveSession> CreateSessionAsync(
            string name,
            string? description,
            int? sceneId,
            int? flowId,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<int> userIds,
            bool includeAssets,
            bool allowGuestLink,
            string? defaultRole,
            CancellationToken cancellationToken);

        Task<List<LiveSession>> ListSessionsAsync(string? status, string? sharedWith, CancellationToken cancellationToken);

        Task<LiveSession> GetSessionAsync(int sessionId, CancellationToken cancellationToken);

        Task<LiveSession> EndSessionAsync(int sessionId, CancellationToken cancellationToken);

        Task<SessionJoinTicketResult> CreateJoinTicketAsync(int sessionId, CancellationToken cancellationToken);

        Task<SessionJoinTicketResult> JoinAsGuestAsync(
            string guestToken,
            string displayName,
            CancellationToken cancellationToken);

        Task<SessionBootstrap> GetBootstrapAsync(int sessionId, CancellationToken cancellationToken);

        Task<SessionBootstrap> GetBootstrapForGuestAsync(string guestToken, CancellationToken cancellationToken);

        Task<SessionAssetManifest> GetSessionAssetsAsync(int sessionId, int sceneId, CancellationToken cancellationToken);

        Task<AssetContent> GetSessionAssetContentAsync(int sessionId, int sceneId, int assetId, CancellationToken cancellationToken);

        Task<AssetContent> GetSessionAssetContentForGuestAsync(
            string guestToken,
            int sceneId,
            int assetId,
            CancellationToken cancellationToken);

        Task<string> GetSessionSceneContentAsync(int sessionId, int sceneId, CancellationToken cancellationToken);

        Task<string> GetSessionSceneContentForGuestAsync(
            string guestToken,
            int sceneId,
            CancellationToken cancellationToken);

        Task MarkRoomStartedAsync(int sessionId, CancellationToken cancellationToken);

        Task MarkRoomEndedAsync(int sessionId, CancellationToken cancellationToken);

        Task<int> EndAbandonedSessionsAsync(CancellationToken cancellationToken);
    }
}
