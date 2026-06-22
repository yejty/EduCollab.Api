namespace EduCollab.Contracts.Responses.Workspaces
{
    public class WorkspacesResponse : PagedCollectionResponse
    {
        public List<WorkspaceResponse> Workspaces { get; set; } = new();
    }
}
