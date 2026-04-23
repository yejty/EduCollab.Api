using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Application.Services.Workspaces
{
    public class WorkspaceService : IWorkspaceService
    {
        public Task<WorkspaceUsersResponse> GetWorkspaceUsersAsync(long workspaceId, CancellationToken cancellationToken)
        {
            // TODO: Load workspace_members joined with Users when workspaces schema exists; return 404 from controller if workspace missing.
            var response = new WorkspaceUsersResponse { Users = new List<WorkspaceUserResponse>() };
            return Task.FromResult(response);
        }
    }
}
