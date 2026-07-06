using EduCollab.Application.Models;
using EduCollab.Application.Services.Flows;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakeFlowService : IFlowService
{
    public Func<Flow, IReadOnlyList<int>, IReadOnlyList<int>?, CancellationToken, Task<bool>>? CreateFlowAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Flow>>>? GetAllFlowsAsyncHandler { get; set; }
    public Func<CancellationToken, Task<List<Flow>>>? GetMyFlowsAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<List<Flow>>>? GetFlowsInGroupAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<Flow?>>? GetFlowByIdAsyncHandler { get; set; }
    public Func<Flow, IReadOnlyList<int>?, IReadOnlyList<int>?, CancellationToken, Task<Flow?>>? UpdateFlowAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? DeleteFlowAsyncHandler { get; set; }
    public Func<int, CancellationToken, Task<bool>>? CanCurrentUserManageFlowAsyncHandler { get; set; }

    public Task<bool> CreateFlowAsync(Flow flow, IReadOnlyList<int> groupIds, IReadOnlyList<int>? sceneIds, CancellationToken cancellationToken) =>
        CreateFlowAsyncHandler?.Invoke(flow, groupIds, sceneIds, cancellationToken) ?? Task.FromResult(true);

    public Task<List<Flow>> GetAllFlowsAsync(CancellationToken cancellationToken) =>
        GetAllFlowsAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Flow>());

    public Task<List<Flow>> GetMyFlowsAsync(CancellationToken cancellationToken) =>
        GetMyFlowsAsyncHandler?.Invoke(cancellationToken) ?? Task.FromResult(new List<Flow>());

    public Task<List<Flow>> GetFlowsInGroupAsync(int groupId, CancellationToken cancellationToken) =>
        GetFlowsInGroupAsyncHandler?.Invoke(groupId, cancellationToken) ?? Task.FromResult(new List<Flow>());

    public Task<Flow?> GetFlowByIdAsync(int flowId, CancellationToken cancellationToken) =>
        GetFlowByIdAsyncHandler?.Invoke(flowId, cancellationToken) ?? Task.FromResult<Flow?>(null);

    public Task<Flow?> UpdateFlowAsync(Flow flow, IReadOnlyList<int>? groupIds, IReadOnlyList<int>? sceneIds, CancellationToken cancellationToken) =>
        UpdateFlowAsyncHandler?.Invoke(flow, groupIds, sceneIds, cancellationToken) ?? Task.FromResult<Flow?>(null);

    public Task<bool> DeleteFlowAsync(int flowId, CancellationToken cancellationToken) =>
        DeleteFlowAsyncHandler?.Invoke(flowId, cancellationToken) ?? Task.FromResult(false);

    public Task<bool> CanCurrentUserManageFlowAsync(int ownerUserId, CancellationToken cancellationToken) =>
        CanCurrentUserManageFlowAsyncHandler?.Invoke(ownerUserId, cancellationToken) ?? Task.FromResult(true);

    public Task<List<int>> GetFlowGroupIdsAsync(int flowId, CancellationToken cancellationToken) =>
        Task.FromResult(new List<int>());

    public Task<List<int>?> SetFlowGroupIdsAsync(int flowId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken) =>
        Task.FromResult<List<int>?>(groupIds.ToList());

    public Task<bool> AddFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<bool> RemoveFlowGroupAsync(int flowId, int groupId, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}
