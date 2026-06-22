namespace EduCollab.Contracts.Responses.Workspaces
{
    public class WorkspaceCreationRequestsResponse : PagedCollectionResponse
    {
        public List<WorkspaceCreationRequestResponse> Requests { get; set; } = new();
    }
}
