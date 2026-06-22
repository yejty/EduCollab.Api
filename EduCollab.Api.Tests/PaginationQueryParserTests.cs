using EduCollab.Api.Query;

namespace EduCollab.Api.Tests;

public sealed class PaginationQueryParserTests
{
    [Fact]
    public void TryParse_UsesDefaults_WhenParametersAreMissing()
    {
        var parsed = PaginationQueryParser.TryParse(null, null, out var specification, out var errorDetail);

        Assert.True(parsed);
        Assert.Equal(PaginationDefaults.DefaultPage, specification.Page);
        Assert.Equal(PaginationDefaults.DefaultPageSize, specification.PageSize);
        Assert.Empty(errorDetail);
    }

    [Fact]
    public void TryParse_RejectsPageSizeAboveMaximum()
    {
        var parsed = PaginationQueryParser.TryParse(1, PaginationDefaults.MaxPageSize + 1, out _, out var errorDetail);

        Assert.False(parsed);
        Assert.Contains(PaginationDefaults.MaxPageSize.ToString(), errorDetail);
    }

    [Fact]
    public void Apply_ReturnsRequestedPageSlice()
    {
        var items = Enumerable.Range(1, 5).ToList();
        var paged = PaginationApplier.Apply(
            items,
            new PaginationSpecification { Page = 2, PageSize = 2 });

        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(3, paged.Items[0]);
        Assert.Equal(4, paged.Items[1]);
        Assert.Equal(2, paged.Page);
        Assert.Equal(2, paged.PageSize);
        Assert.Equal(5, paged.TotalCount);
    }
}
