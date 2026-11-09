using EduCollab.Application.Services.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EduCollab.Api.MinimalApis;

internal sealed class InternalApiKeyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var settings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<InternalApiSettings>>()
            .Value;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return Results.Json(
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Internal API key is not configured.",
                    Type = "https://httpstatuses.com/503",
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var provided)
            || !string.Equals(provided.ToString(), settings.ApiKey, StringComparison.Ordinal))
        {
            return Results.Json(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Invalid or missing X-Api-Key.",
                    Type = "https://httpstatuses.com/401",
                },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
