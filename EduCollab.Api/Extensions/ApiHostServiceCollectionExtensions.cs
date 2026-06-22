using EduCollab.Api.ExceptionHandlers;
using EduCollab.Api.Health;
using EduCollab.Api.Middleware;
using EduCollab.Api.Problems;
using EduCollab.Api.Security;
using Microsoft.AspNetCore.Authorization;
using EduCollab.Api.Swagger;
using EduCollab.Application.Identity;
using Microsoft.AspNetCore.Mvc;
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
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = ApiProblemDetailsFactory.CreateValidationProblem(
                        context.HttpContext,
                        context.ModelState);

                    return new BadRequestObjectResult(problem)
                    {
                        ContentTypes = { ApiProblemDetailsFactory.ProblemJsonMediaType },
                    };
                };
            });
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            services.AddEduCollabOpenApi();

            return services;
        }
    }
}
