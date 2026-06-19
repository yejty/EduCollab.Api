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
        /// Group that receives access to this scene when it is created.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "GroupId is required.")]
        public int GroupId { get; set; }
    }
}
