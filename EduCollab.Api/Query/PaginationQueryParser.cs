namespace EduCollab.Api.Query
{
    public static class PaginationDefaults
    {
        public const int DefaultPage = 1;

        public const int DefaultPageSize = 20;

        public const int MaxPageSize = 100;
    }

    public sealed class PaginationSpecification
    {
        public int Page { get; init; }

        public int PageSize { get; init; }
    }

    public sealed class PagedResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; init; }
    }

    public static class PaginationQueryParser
    {
        public static bool TryParse(
            int? page,
            int? pageSize,
            out PaginationSpecification specification,
            out string errorDetail)
        {
            var resolvedPage = page ?? PaginationDefaults.DefaultPage;
            var resolvedPageSize = pageSize ?? PaginationDefaults.DefaultPageSize;

            if (resolvedPage < 1)
            {
                specification = new PaginationSpecification
                {
                    Page = PaginationDefaults.DefaultPage,
                    PageSize = PaginationDefaults.DefaultPageSize,
                };
                errorDetail = "Page must be greater than or equal to 1.";
                return false;
            }

            if (resolvedPageSize < 1)
            {
                specification = new PaginationSpecification
                {
                    Page = PaginationDefaults.DefaultPage,
                    PageSize = PaginationDefaults.DefaultPageSize,
                };
                errorDetail = "PageSize must be greater than or equal to 1.";
                return false;
            }

            if (resolvedPageSize > PaginationDefaults.MaxPageSize)
            {
                specification = new PaginationSpecification
                {
                    Page = PaginationDefaults.DefaultPage,
                    PageSize = PaginationDefaults.DefaultPageSize,
                };
                errorDetail = $"PageSize must not exceed {PaginationDefaults.MaxPageSize}.";
                return false;
            }

            specification = new PaginationSpecification
            {
                Page = resolvedPage,
                PageSize = resolvedPageSize,
            };
            errorDetail = string.Empty;
            return true;
        }
    }

    public static class PaginationApplier
    {
        public static PagedResult<T> Apply<T>(IReadOnlyList<T> items, PaginationSpecification pagination)
        {
            var skip = (pagination.Page - 1) * pagination.PageSize;
            var pageItems = items.Skip(skip).Take(pagination.PageSize).ToList();

            return new PagedResult<T>
            {
                Items = pageItems,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = items.Count,
            };
        }
    }
}
