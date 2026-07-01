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

        /// <summary>
        /// Inline scene document. Objects that use a workspace asset include an <c>assetId</c> property
        /// anywhere in the JSON tree (for example under <c>objects[]</c>).
        /// </summary>
        [Required]
        public JsonNode? JsonContent { get; set; }

        /// <summary>
        /// Primary group placement when creating the scene.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Groups that receive access to this scene when it is created.
        /// When provided, takes precedence over <see cref="GroupId"/>.
        /// </summary>
        public List<int>? GroupIds { get; set; }
    }
}
