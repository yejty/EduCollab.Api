using System.Text.Json.Nodes;

namespace EduCollab.Contracts.Responses.Scenes
{
    public class SceneResponse
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int OwnerUserId { get; set; }
        public int GroupId { get; set; }
        public List<int> GroupIds { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public JsonNode? JsonContent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
        public bool CanManage { get; set; }
    }
}
