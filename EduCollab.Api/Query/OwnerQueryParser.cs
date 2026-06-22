namespace EduCollab.Api.Query
{
    public static class OwnerQueryParser
    {
        public const string MeValue = "me";

        public static bool TryParse(string? owner, out bool filterToCurrentUser, out string? errorDetail)
        {
            filterToCurrentUser = false;

            if (string.IsNullOrWhiteSpace(owner))
            {
                errorDetail = null;
                return true;
            }

            if (string.Equals(owner.Trim(), MeValue, StringComparison.OrdinalIgnoreCase))
            {
                filterToCurrentUser = true;
                errorDetail = null;
                return true;
            }

            errorDetail = "owner must be 'me' when specified.";
            return false;
        }
    }
}
