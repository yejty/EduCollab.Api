namespace EduCollab.Contracts.Responses.Scenes
{
    public class SceneVersionResponse
    {
        public int SceneId { get; set; }
        public int VersionNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ETag { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
