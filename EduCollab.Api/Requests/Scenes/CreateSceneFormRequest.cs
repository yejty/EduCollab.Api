namespace EduCollab.Api.Requests.Scenes
{
    public sealed class CreateSceneFormRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Groups that receive access to this scene when it is created.
        /// Omit to keep the scene in your personal space.
        /// </summary>
        public List<int>? GroupIds { get; set; }

        /// <summary>
        /// Inline scene JSON when not uploading a <see cref="JsonFile"/>.
        /// </summary>
        public string? JsonContent { get; set; }

        /// <summary>
        /// Optional scene JSON file (alternative to <see cref="JsonContent"/>).
        /// </summary>
        public IFormFile? JsonFile { get; set; }
    }
}
