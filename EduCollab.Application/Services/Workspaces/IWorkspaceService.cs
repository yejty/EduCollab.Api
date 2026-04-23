using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<WorkspaceUsersResponse> GetWorkspaceUsersAsync(long workspaceId, CancellationToken cancellationToken);
    }
}
