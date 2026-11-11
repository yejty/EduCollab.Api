using System.ComponentModel.DataAnnotations;

namespace Space.Contracts.Requests.Assets
{
    public class ShareWithGroupRequest
    {
        [Range(1, int.MaxValue)]
        public int GroupId { get; set; }
    }
}
