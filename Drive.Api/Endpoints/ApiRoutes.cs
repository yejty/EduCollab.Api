using System.Globalization;
using System.Reflection;

namespace Drive.Api.MinimalApis;

internal static class ApiRoutes
{
    public static string For(string template, object? routeValues = null)
    {
        var path = template;
        if (routeValues is not null)
        {
            foreach (var property in routeValues.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var text = Convert.ToString(property.GetValue(routeValues), CultureInfo.InvariantCulture) ?? string.Empty;
                path = path.Replace("{" + property.Name + "}", Uri.EscapeDataString(text), StringComparison.OrdinalIgnoreCase);
            }
        }

        return "/" + path.TrimStart('/');
    }
}
