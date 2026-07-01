namespace EduCollab.Application.Models
{
    public class FlowSceneLink
    {
        public int FlowId { get; set; }
        public int SceneId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
