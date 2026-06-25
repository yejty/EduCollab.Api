using EduCollab.Application.Models;

namespace EduCollab.Application.Repositories
{
    public interface IFlowRepository
    {
        Task<int> CreateFlowAsync(int workspaceId, Flow flow, CancellationToken cancellationToken);
        Task<List<Flow>> GetAllFlowsAsync(int workspaceId, CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsByGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken);
        Task<List<Flow>> GetFlowsByOwnerAsync(int workspaceId, int ownerUserId, CancellationToken cancellationToken);
        Task<Flow?> GetFlowByIdAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<Flow?> UpdateFlowAsync(int workspaceId, Flow flow, CancellationToken cancellationToken);
        Task<bool> DeleteFlowAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<List<FlowScene>> GetFlowScenesAsync(int workspaceId, int flowId, CancellationToken cancellationToken);
        Task<FlowScene?> AddFlowSceneAsync(int workspaceId, FlowScene flowScene, CancellationToken cancellationToken);
        Task<bool> RemoveFlowSceneAsync(int workspaceId, int flowId, int sceneId, CancellationToken cancellationToken);
    }
}
