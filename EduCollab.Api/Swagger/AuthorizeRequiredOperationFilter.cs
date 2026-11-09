using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EduCollab.Api.Swagger
{
    /// <summary>
    /// Adds the Bearer security requirement only for endpoints that require authorization,
    /// mirroring MVC rules for <see cref="AuthorizeAttribute"/> and <see cref="AllowAnonymousAttribute"/>.
    /// </summary>
    public sealed class AuthorizeRequiredOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!RequiresAuthorization(context))
                return;

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    }] = Array.Empty<string>(),
                },
            ];
        }

        private static bool RequiresAuthorization(OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IAllowAnonymous>().Any())
                return false;

            if (metadata.OfType<IAuthorizeData>().Any())
                return true;

            var methodInfo = context.MethodInfo;
            if (methodInfo == null)
                return false;

            if (methodInfo.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any())
                return false;

            if (methodInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any())
                return true;

            var controllerType = methodInfo.DeclaringType;
            if (controllerType?.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any() == true)
                return false;

            return controllerType?.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any() == true;
        }
    }
}
