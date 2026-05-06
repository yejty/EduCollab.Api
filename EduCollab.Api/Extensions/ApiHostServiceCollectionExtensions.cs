using EduCollab.Api.ExceptionHandlers;
using EduCollab.Api.Health;
using EduCollab.Api.Swagger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace EduCollab.Api.Extensions
{
    public static class ApiHostServiceCollectionExtensions
    {
        public static IServiceCollection AddApiHost(this IServiceCollection services)
        {
            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddProblemDetails();
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT bearer token. In Swagger UI, use Authorize and paste the access token from login.",
                });

                options.OperationFilter<AuthorizeRequiredOperationFilter>();
            });

            return services;
        }
    }
}
