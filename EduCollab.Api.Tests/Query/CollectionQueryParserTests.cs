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

public class AssetListQueryParserTests
{
    [Fact]
    public void TryParse_rejects_owner_with_groupId()
    {
        var ok = AssetListQueryParser.TryParse("me", null, 7, out _, out var error);

        Assert.False(ok);
        Assert.Equal("owner cannot be combined with folderId or groupId.", error);
    }

    [Fact]
    public void TryParse_accepts_group_and_folder()
    {
        var ok = AssetListQueryParser.TryParse(null, 3, 7, out var filter, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(3, filter.FolderId);
        Assert.Equal(7, filter.GroupId);
    }
}

public class AssetFolderListQueryParserTests
{
    [Fact]
    public void TryParse_rejects_non_positive_groupId()
    {
        var ok = AssetFolderListQueryParser.TryParse(0, null, out _, out var error);

        Assert.False(ok);
        Assert.Equal("groupId must be a positive integer when specified.", error);
    }
}
