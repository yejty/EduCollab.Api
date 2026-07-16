namespace EduCollab.Contracts.Requests.Groups
{
    public class AttachFlowGroupRequest
    {
        public int FlowId { get; set; }

        public int GroupId { get; set; }

        public bool IncludeAssets { get; set; }
    }
}
