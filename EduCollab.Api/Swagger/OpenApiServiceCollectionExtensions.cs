using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace EduCollab.Api.Swagger
{
    public static class OpenApiServiceCollectionExtensions
    {
        public static IServiceCollection AddEduCollabOpenApi(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(OpenApiContractDescriptions.DocumentName, new OpenApiInfo
                {
                    Title = OpenApiContractDescriptions.Title,
                    Version = OpenApiContractDescriptions.Version,
                    Description = OpenApiContractDescriptions.BuildInfoDescription(),
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

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
                options.OperationFilter<ListQueryParameterOperationFilter>();
                options.SchemaFilter<PasswordExampleSchemaFilter>();
                options.DocumentFilter<ProblemDetailsDocumentFilter>();
            });

            return services;
        }
    }
}
