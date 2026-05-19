using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int CreatedByUserId { get; set; }
        public int UserCount { get; set; }
        public string? CurrentUserRole { get; set; }
    }
}
