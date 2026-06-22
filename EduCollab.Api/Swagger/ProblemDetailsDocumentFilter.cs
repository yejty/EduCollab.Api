using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EduCollab.Api.Swagger
{
    public sealed class ProblemDetailsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Components ??= new OpenApiComponents();
            swaggerDoc.Components.Schemas["ApiProblemDetails"] = CreateApiProblemDetailsSchema();
            swaggerDoc.Components.Schemas["ApiValidationProblemDetails"] = CreateApiValidationProblemDetailsSchema();
        }

        private static OpenApiSchema CreateApiProblemDetailsSchema() => new()
        {
            Type = "object",
            Description = "RFC 9457 problem response emitted by EduCollab API endpoints.",
            Properties = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal)
            {
                ["type"] = new OpenApiSchema { Type = "string", Description = "Problem type URI, e.g. urn:educollab:error:invalid_sort." },
                ["title"] = new OpenApiSchema { Type = "string" },
                ["status"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                ["detail"] = new OpenApiSchema { Type = "string" },
                ["instance"] = new OpenApiSchema { Type = "string", Description = "Request path that produced the error." },
                ["error"] = new OpenApiSchema { Type = "string", Description = "Machine-readable error code." },
                ["requestId"] = new OpenApiSchema { Type = "string", Description = "Server-generated request identifier; also returned as X-Request-Id." },
            },
            Required = new HashSet<string> { "type", "title", "status", "error", "requestId" },
        };

        private static OpenApiSchema CreateApiValidationProblemDetailsSchema()
        {
            var schema = CreateApiProblemDetailsSchema();
            schema.Description = "Validation problem response (error code validation_failed).";
            schema.Properties["errors"] = new OpenApiSchema
            {
                Type = "object",
                Description = "Field-level validation messages keyed by camelCase field name.",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string" },
                },
            };
            return schema;
        }
    }
}
