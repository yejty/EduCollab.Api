using EduCollab.Contracts.Responses.Workspaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Contracts.Responses.Groups
{
    public class GroupsResponse
    {
        public List<GroupResponse> Groups { get; set; } = new();
    }
}
