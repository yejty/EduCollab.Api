using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace EduCollab.Contracts.Requests.Scenes
{
    public class CreateSceneRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public JsonNode? JsonContent { get; set; }

        /// <summary>
        /// Optional group to share with on create. When omitted, the scene is private to the creator and workspace owner.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? GroupId { get; set; }
    }
}
