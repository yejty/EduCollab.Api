using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EduCollab.Api.MinimalApis;

internal sealed class RequestValidationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var method = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<MethodInfo>();
        var parameters = method?.GetParameters();
        if (parameters is null)
        {
            return await next(context);
        }

        var modelState = new ModelStateDictionary();
        var count = Math.Min(parameters.Length, context.Arguments.Count);
        for (var index = 0; index < count; index++)
        {
            var parameter = parameters[index];
            var argument = context.Arguments[index];
            if (!IsRequestParameter(parameter))
            {
                continue;
            }

            if (parameter.ParameterType == typeof(JsonNode))
            {
                if (argument is null)
                {
                    modelState.AddModelError(parameter.Name ?? string.Empty, "A non-empty request body is required.");
                }

                continue;
            }

            if (argument is null)
            {
                if (parameter.GetCustomAttribute<FromFormAttribute>() is null)
                {
                    modelState.AddModelError(parameter.Name ?? string.Empty, "A non-empty request body is required.");
                }

                continue;
            }

            RequestValidation.TryValidate(argument, modelState);
        }

        if (modelState.ErrorCount > 0)
        {
            return ApiResults.Validation(context.HttpContext, modelState);
        }

        return await next(context);
    }

    private static bool IsRequestParameter(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<FromQueryAttribute>() is not null
            || parameter.GetCustomAttribute<FromRouteAttribute>() is not null
            || parameter.GetCustomAttribute<FromHeaderAttribute>() is not null
            || parameter.GetCustomAttribute<FromServicesAttribute>() is not null)
        {
            return false;
        }

        var type = parameter.ParameterType;
        if (type.IsValueType || type == typeof(string) || type == typeof(IFormFile) || type == typeof(CancellationToken))
        {
            return false;
        }

        return type == typeof(JsonNode) || RequestValidation.IsRequestContract(type);
    }
}
