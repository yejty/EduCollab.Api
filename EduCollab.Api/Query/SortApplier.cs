namespace EduCollab.Api.Query
{
    public static class SortApplier
    {
        public static List<T> Apply<T>(
            IEnumerable<T> items,
            SortSpecification sort,
            IReadOnlyDictionary<string, Func<T, object>> fieldSelectors,
            Func<T, object>? tieBreaker = null)
        {
            if (!fieldSelectors.TryGetValue(sort.Field, out var primarySelector))
            {
                throw new ArgumentException($"Unsupported sort field '{sort.Field}'.", nameof(sort));
            }

            IEnumerable<T> ordered = sort.Direction == SortDirection.Asc
                ? items.OrderBy(primarySelector, SortValueComparer.Instance)
                : items.OrderByDescending(primarySelector, SortValueComparer.Instance);

            if (tieBreaker is not null)
            {
                ordered = sort.Direction == SortDirection.Asc
                    ? ((IOrderedEnumerable<T>)ordered).ThenBy(tieBreaker, SortValueComparer.Instance)
                    : ((IOrderedEnumerable<T>)ordered).ThenByDescending(tieBreaker, SortValueComparer.Instance);
            }

            return ordered.ToList();
        }

        private sealed class SortValueComparer : IComparer<object>
        {
            public static SortValueComparer Instance { get; } = new();

            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x is null)
                {
                    return -1;
                }

                if (y is null)
                {
                    return 1;
                }

                return x switch
                {
                    string left when y is string right => string.Compare(left, right, StringComparison.OrdinalIgnoreCase),
                    IComparable comparable => comparable.CompareTo(y),
                    _ => throw new InvalidOperationException($"Unsupported sort value type '{x.GetType().Name}'."),
                };
            }
        }
    }
}
