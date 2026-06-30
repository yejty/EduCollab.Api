using EduCollab.Api.Query;

namespace EduCollab.Api.Tests.Query;

public class OwnerQueryParserTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("me", true)]
    [InlineData("ME", true)]
    public void TryParse_accepts_me_or_empty(string? owner, bool expectedFilter)
    {
        var ok = OwnerQueryParser.TryParse(owner, out var filterToCurrentUser, out var error);

        Assert.True(ok);
        Assert.Equal(expectedFilter, filterToCurrentUser);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_rejects_unknown_owner()
    {
        var ok = OwnerQueryParser.TryParse("someone-else", out _, out var error);

        Assert.False(ok);
        Assert.Equal("owner must be 'me' when specified.", error);
    }
}

public class GroupListViewQueryParserTests
{
    [Theory]
    [InlineData(null, GroupListView.Tree)]
    [InlineData("", GroupListView.Tree)]
    [InlineData("tree", GroupListView.Tree)]
    [InlineData("TREE", GroupListView.Tree)]
    [InlineData("flat", GroupListView.Flat)]
    [InlineData("FLAT", GroupListView.Flat)]
    public void TryParse_accepts_tree_or_flat(string? view, GroupListView expectedView)
    {
        var ok = GroupListViewQueryParser.TryParse(view, out var listView, out var error);

        Assert.True(ok);
        Assert.Equal(expectedView, listView);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_rejects_unknown_view()
    {
        var ok = GroupListViewQueryParser.TryParse("nested", out _, out var error);

        Assert.False(ok);
        Assert.Equal("view must be 'flat' or 'tree' when specified.", error);
    }
}
