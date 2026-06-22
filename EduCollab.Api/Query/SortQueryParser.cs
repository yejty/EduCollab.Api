namespace EduCollab.Api.Query
{
    public enum SortDirection
    {
        Asc,
        Desc,
    }

    public sealed class SortSpecification
    {
        public required string Field { get; init; }

        public SortDirection Direction { get; init; }
    }

    public static class SortQueryParser
    {
        public static bool TryParse(
            string? sort,
            IReadOnlyCollection<string> allowedFields,
            SortSpecification defaultSort,
            out SortSpecification specification,
            out string errorDetail)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                specification = defaultSort;
                errorDetail = string.Empty;
                return true;
            }

            var trimmed = sort.Trim();
            var descending = trimmed.StartsWith('-');
            var field = descending ? trimmed[1..] : trimmed;

            if (string.IsNullOrWhiteSpace(field))
            {
                specification = defaultSort;
                errorDetail = "Sort field must not be empty.";
                return false;
            }

            if (!allowedFields.Contains(field, StringComparer.Ordinal))
            {
                specification = defaultSort;
                errorDetail = $"Sort field must be one of: {string.Join(", ", allowedFields.OrderBy(static f => f))}.";
                return false;
            }

            specification = new SortSpecification
            {
                Field = field,
                Direction = descending ? SortDirection.Desc : SortDirection.Asc,
            };
            errorDetail = string.Empty;
            return true;
        }
    }
}
