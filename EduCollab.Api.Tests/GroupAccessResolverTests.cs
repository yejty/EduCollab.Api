using EduCollab.Application.Models;
using EduCollab.Application.Services.Groups;

namespace EduCollab.Api.Tests;

public sealed class GroupAccessResolverTests
{
    [Fact]
    public void ExpandToDescendants_includesDirectMembershipAndChildGroups()
    {
        var groups = new List<Group>
        {
            new() { Id = 1, Name = "Science", ParentGroupId = null },
            new() { Id = 2, Name = "Physics", ParentGroupId = 1 },
            new() { Id = 3, Name = "Chemistry", ParentGroupId = 1 },
            new() { Id = 4, Name = "Arts", ParentGroupId = null },
        };

        var accessible = GroupAccessResolver.ExpandToDescendants([1], groups);

        Assert.Equal(3, accessible.Count);
        Assert.Contains(1, accessible);
        Assert.Contains(2, accessible);
        Assert.Contains(3, accessible);
        Assert.DoesNotContain(4, accessible);
    }
}
