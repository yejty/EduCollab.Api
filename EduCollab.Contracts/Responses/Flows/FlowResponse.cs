namespace EduCollab.Contracts.Responses.Flows
{
    public class FlowResponse
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int OwnerUserId { get; set; }
        public int GroupId { get; set; }
        public List<int> GroupIds { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CanManage { get; set; }
    }
}
