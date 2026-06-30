namespace EduCollab.Contracts.Requests.Scenes
{
    public class UpdateSceneRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GroupId { get; set; }

        /// <summary>
        /// Inline scene document. Objects that use a workspace asset include an <c>assetId</c> property
        /// anywhere in the JSON tree (for example under <c>objects[]</c>).
        /// </summary>
        public System.Text.Json.Nodes.JsonNode? JsonContent { get; set; }
    }
}
