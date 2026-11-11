using Hub.Api.Query;

namespace Hub.Api.MinimalApis;

internal static class EndpointQuery
{
    public static bool TryParseList(
        HttpContext httpContext,
        string? sort,
        int? page,
        int? pageSize,
        IReadOnlyCollection<string> allowedSortFields,
        SortSpecification defaultSort,
        out SortSpecification sortSpecification,
        out PaginationSpecification paginationSpecification,
        out IResult? problem)
    {
        if (!SortQueryParser.TryParse(sort, allowedSortFields, defaultSort, out sortSpecification, out var sortError))
        {
            paginationSpecification = null!;
            problem = ApiResults.BadRequest(httpContext, "invalid_sort", sortError);
            return false;
        }

        if (!PaginationQueryParser.TryParse(page, pageSize, out paginationSpecification, out var pageError))
        {
            problem = ApiResults.BadRequest(httpContext, "invalid_pagination", pageError);
            return false;
        }

        problem = null;
        return true;
    }

    public static IResult? InvalidGroupId(HttpContext httpContext, int groupId) =>
        groupId <= 0
            ? ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId must be a positive integer.")
            : null;
}
