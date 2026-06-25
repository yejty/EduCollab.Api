namespace EduCollab.Contracts.Requests.Scenes
{
    public class UpdateSceneRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GroupId { get; set; }
        public System.Text.Json.Nodes.JsonNode? JsonContent { get; set; }
    }
}
