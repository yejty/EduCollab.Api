using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IFlowRepository
    {
        Task<int> CreateFlowAsync(int workspaceId, Flow flow, CancellationToken cancellationToken);
        Task<List<Flow>> GetAllFlowsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<Flow?> GetFlowByIdAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<Flow?> UpdateFlowAsync(int workspaceId, Flow flow, CancellationToken cancellationToken);
        Task<bool> DeleteFlowAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<List<FlowSceneLink>> GetFlowSceneLinksAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<Dictionary<int, List<int>>> GetFlowSceneIdsByFlowIdsAsync(int workspaceId, IReadOnlyCollection<int> flowIds, CancellationToken cancellationToken);
        Task ReplaceFlowSceneLinksAsync(int workspaceId, int flowId, IReadOnlyList<int> sceneIds, int createdByUserId, CancellationToken cancellationToken);
        Task<List<int>> GetFlowGroupIdsAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<List<FlowGroupShare>> GetFlowGroupSharesAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<Dictionary<int, List<int>>> GetFlowGroupIdsByFlowIdsAsync(int workspaceId, IReadOnlyCollection<int> flowIds, CancellationToken cancellationToken);
        Task ReplaceFlowGroupSharesAsync(int workspaceId, int flowId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken);
        Task ReplaceFlowGroupSharesAsync(int workspaceId, int flowId, IReadOnlyList<FlowGroupShare> shares, CancellationToken cancellationToken);
        Task<bool> AddFlowGroupShareAsync(int workspaceId, int flowId, int groupId, CancellationToken cancellationToken);
        Task<bool> AddFlowGroupShareAsync(int workspaceId, int flowId, int groupId, bool includeAssets, CancellationToken cancellationToken);
        Task<bool> RemoveFlowGroupShareAsync(int workspaceId, int flowId, int groupId, CancellationToken cancellationToken);
        Task SyncFlowPrimaryGroupIdAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
    }
}
