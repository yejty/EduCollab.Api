namespace EduCollab.Contracts.Requests.Flows
{
    public class CreateFlowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Groups that receive access to this flow when it is created.
        /// Omit to keep the flow in your personal space.
        /// </summary>
        public List<int>? GroupIds { get; set; }
        public List<int>? SceneIds { get; set; }
    }
}
