using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace EduCollab.Contracts.Requests.Scenes
{
    public class UpdateSceneRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public JsonNode? JsonContent { get; set; }
    }
}
