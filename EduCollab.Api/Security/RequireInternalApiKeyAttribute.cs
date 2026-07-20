using EduCollab.Application.Services.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EduCollab.Api.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireInternalApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var settings = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<InternalApiSettings>>()
                .Value;

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Internal API key is not configured.",
                    Type = "https://httpstatuses.com/503",
                })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                };
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var provided)
                || !string.Equals(provided.ToString(), settings.ApiKey, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Invalid or missing X-Api-Key.",
                    Type = "https://httpstatuses.com/401",
                });
            }
        }
    }
}
