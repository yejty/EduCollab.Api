namespace EduCollab.Api.Http
{
    public static class EntityTagHeaderParser
    {
        public static string Normalize(string rawValue)
        {
            var trimmed = rawValue.Trim();
            if (trimmed.StartsWith("W/", StringComparison.Ordinal))
            {
                trimmed = trimmed[2..].Trim();
            }

            if (trimmed.Length >= 2 && trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            {
                trimmed = trimmed[1..^1];
            }

            return trimmed;
        }
    }
}
