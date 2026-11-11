using Hub.Api.ExceptionHandlers;
using Hub.Api.Health;
using Hub.Api.Middleware;
using Hub.Api.Problems;
using Hub.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Hub.Api.Swagger;
using Space.Application.Identity;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Api.Extensions
{
    public static class ApiHostServiceCollectionExtensions
    {
        public static IServiceCollection AddApiHost(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    ApiProblemDetailsCustomizer.Apply(context.HttpContext, context.ProblemDetails);
                };
            });
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            services.Configure<RouteHandlerOptions>(options =>
            {
                options.ThrowOnBadRequest = true;
            });
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);
            services.AddSpaceOpenApi();
            services.AddHostedService<Services.AbandonedSessionCleanupService>();

            return services;
        }
    }
}
