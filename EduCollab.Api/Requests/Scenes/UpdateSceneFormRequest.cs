namespace EduCollab.Api.Requests.Scenes
{
    public sealed class UpdateSceneFormRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int GroupId { get; set; }

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
