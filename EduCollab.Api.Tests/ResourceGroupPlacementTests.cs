using EduCollab.Application.Services.Content;

namespace EduCollab.Api.Tests;

public sealed class ResourceGroupPlacementTests
{
    [Fact]
    public void ResolveGroupIds_ReturnsEmpty_WhenGroupIdsIsNull()
    {
        var result = ResourceGroupPlacement.ResolveGroupIds(null);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveGroupIds_ReturnsEmpty_WhenGroupIdsIsEmpty()
    {
        var result = ResourceGroupPlacement.ResolveGroupIds([]);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveGroupIds_ReturnsDistinctPositiveIds()
    {
        var result = ResourceGroupPlacement.ResolveGroupIds([3, 1, 3, 2]);

        Assert.Equal([3, 1, 2], result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolveGroupIds_ThrowsArgumentException_WhenGroupIdIsNotPositive(int invalidGroupId)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ResourceGroupPlacement.ResolveGroupIds([invalidGroupId]));

        Assert.Equal("groupIds", exception.ParamName);
        Assert.Contains("positive integer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveGroupIds_ThrowsArgumentException_WhenGroupIdsContainZero()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ResourceGroupPlacement.ResolveGroupIds([1, 0]));

        Assert.Equal("groupIds", exception.ParamName);
    }

    [Fact]
    public void PrimaryGroupId_ReturnsFirstGroupId_WhenListIsNotEmpty()
    {
        Assert.Equal(5, ResourceGroupPlacement.PrimaryGroupId([5, 9]));
    }

    [Fact]
    public void PrimaryGroupId_ReturnsZero_WhenListIsEmpty()
    {
        Assert.Equal(0, ResourceGroupPlacement.PrimaryGroupId([]));
    }
}
