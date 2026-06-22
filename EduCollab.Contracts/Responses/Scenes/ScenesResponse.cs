namespace EduCollab.Contracts.Responses.Scenes
{
    public class ScenesResponse : PagedCollectionResponse
    {
        public List<SceneResponse> Scenes { get; set; } = new();
    }
}
