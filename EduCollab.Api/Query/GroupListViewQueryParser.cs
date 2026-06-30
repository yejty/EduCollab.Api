namespace EduCollab.Api.Query
{
    public enum GroupListView
    {
        Tree,
        Flat,
    }

    public static class GroupListViewQueryParser
    {
        public const string FlatValue = "flat";
        public const string TreeValue = "tree";

        public static bool TryParse(string? view, out GroupListView listView, out string? errorDetail)
        {
            listView = GroupListView.Tree;

            if (string.IsNullOrWhiteSpace(view))
            {
                errorDetail = null;
                return true;
            }

            var trimmed = view.Trim();
            if (string.Equals(trimmed, FlatValue, StringComparison.OrdinalIgnoreCase))
            {
                listView = GroupListView.Flat;
                errorDetail = null;
                return true;
            }

            if (string.Equals(trimmed, TreeValue, StringComparison.OrdinalIgnoreCase))
            {
                listView = GroupListView.Tree;
                errorDetail = null;
                return true;
            }

            errorDetail = "view must be 'flat' or 'tree' when specified.";
            return false;
        }
    }
}
