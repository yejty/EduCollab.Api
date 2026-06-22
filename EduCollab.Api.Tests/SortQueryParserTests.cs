using EduCollab.Api.Query;
using EduCollab.Application.Models;

namespace EduCollab.Api.Tests;

public sealed class SortQueryParserTests
{
    [Fact]
    public void TryParse_UsesDefault_WhenSortIsMissing()
    {
        var defaultSort = new SortSpecification { Field = "name", Direction = SortDirection.Asc };

        var parsed = SortQueryParser.TryParse(
            null,
            ["name", "id"],
            defaultSort,
            out var specification,
            out var errorDetail);

        Assert.True(parsed);
        Assert.Equal("name", specification.Field);
        Assert.Equal(SortDirection.Asc, specification.Direction);
        Assert.Empty(errorDetail);
    }

    [Fact]
    public void TryParse_ParsesDescendingPrefix()
    {
        var defaultSort = new SortSpecification { Field = "name", Direction = SortDirection.Asc };

        var parsed = SortQueryParser.TryParse(
            "-createdAt",
            ["createdAt", "name"],
            defaultSort,
            out var specification,
            out _);

        Assert.True(parsed);
        Assert.Equal("createdAt", specification.Field);
        Assert.Equal(SortDirection.Desc, specification.Direction);
    }

    [Fact]
    public void TryParse_RejectsUnknownField()
    {
        var defaultSort = new SortSpecification { Field = "name", Direction = SortDirection.Asc };

        var parsed = SortQueryParser.TryParse(
            "unknown",
            ["name", "id"],
            defaultSort,
            out _,
            out var errorDetail);

        Assert.False(parsed);
        Assert.Contains("name", errorDetail);
        Assert.Contains("id", errorDetail);
    }

    [Fact]
    public void ApplyAssets_SortsByDescendingCreatedAt()
    {
        var assets = new List<Asset>
        {
            new() { Id = 1, Name = "A", CreatedAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "B", CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        };

        var sorted = ResourceSortProfiles.NamedResource.ApplyAssets(
            assets,
            new SortSpecification { Field = "createdAt", Direction = SortDirection.Desc });

        Assert.Equal(2, sorted[0].Id);
        Assert.Equal(1, sorted[1].Id);
    }
}
