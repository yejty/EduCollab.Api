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
