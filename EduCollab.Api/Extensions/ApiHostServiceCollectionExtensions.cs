using EduCollab.Api.ExceptionHandlers;
using EduCollab.Api.Health;
using EduCollab.Api.Middleware;
using EduCollab.Api.Problems;
using EduCollab.Api.Security;
using Microsoft.AspNetCore.Authorization;
using EduCollab.Api.Swagger;
using EduCollab.Application.Identity;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Api.Extensions
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
            services.AddEduCollabOpenApi();
            services.AddHostedService<Services.AbandonedSessionCleanupService>();

            return services;
        }
    }
}
