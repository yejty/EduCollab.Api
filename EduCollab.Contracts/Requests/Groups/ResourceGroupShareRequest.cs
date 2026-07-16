namespace EduCollab.Contracts.Requests.Groups
{
    public class ResourceGroupShareRequest
    {
        public int GroupId { get; set; }

        public bool IncludeAssets { get; set; }
    }
}
