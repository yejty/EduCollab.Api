using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Hub.Api.MinimalApis;

internal static class RequestValidation
{
    public static bool TryValidate(object instance, ModelStateDictionary modelState) =>
        TryValidate(instance, modelState, prefix: null, new HashSet<object>(ReferenceEqualityComparer.Instance));

    public static bool IsRequestContract(Type type)
    {
        var namespaceName = type.Namespace;
        return namespaceName is not null
            && (namespaceName.StartsWith("Space.Contracts.Requests", StringComparison.Ordinal)
                || namespaceName.StartsWith("Hub.Api.Requests", StringComparison.Ordinal));
    }

    private static bool TryValidate(
        object instance,
        ModelStateDictionary modelState,
        string? prefix,
        HashSet<object> visited)
    {
        if (!visited.Add(instance))
        {
            return modelState.ErrorCount == 0;
        }

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        foreach (var result in results)
        {
            var message = result.ErrorMessage ?? "The value is invalid.";
            var members = result.MemberNames?.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            if (members is null || members.Length == 0)
            {
                modelState.AddModelError(prefix ?? string.Empty, message);
                continue;
            }

            foreach (var member in members)
            {
                modelState.AddModelError(Combine(prefix, member), message);
            }
        }

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(instance);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (value is null || value is string || value is IFormFile)
            {
                continue;
            }

            if (value is IEnumerable enumerable)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    if (item is not null && IsRequestContract(item.GetType()))
                    {
                        TryValidate(item, modelState, Combine(prefix, $"{property.Name}[{index}]"), visited);
                    }

                    index++;
                }

                continue;
            }

            if (IsRequestContract(value.GetType()))
            {
                TryValidate(value, modelState, Combine(prefix, property.Name), visited);
            }
        }

        return modelState.ErrorCount == 0;
    }

    private static string Combine(string? prefix, string name) =>
        string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
}
