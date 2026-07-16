using EduCollab.Application.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EduCollab.Api.Swagger
{
    public sealed class RequiresWorkspaceParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo?
                .GetCustomAttributes(typeof(RequiresWorkspaceParameterAttribute), inherit: true)
                .OfType<RequiresWorkspaceParameterAttribute>()
                .FirstOrDefault();

            if (attribute is null)
                return;

            var lines = new List<string>();
            if (attribute.MembershipOnly)
            {
                lines.Add("**Workspace parameters:** active workspace membership (any member).");
            }
            else if (attribute.Parameters.Length > 0)
            {
                var parameterLabels = attribute.Parameters
                    .Select(FormatParameter)
                    .ToList();

                var joiner = attribute.RequireAll ? " and " : " or ";
                lines.Add($"**Workspace parameters:** {string.Join(joiner, parameterLabels)}.");
            }

            if (attribute.EditWorkspaceBypass && !attribute.MembershipOnly)
            {
                lines.Add("**Bypass:** `editWorkspace` grants full workspace access.");
            }

            if (!string.IsNullOrWhiteSpace(attribute.Notes))
            {
                lines.Add($"**Notes:** {attribute.Notes.Trim()}");
            }

            if (lines.Count == 0)
                return;

            var block = string.Join('\n', lines);
            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? block
                : $"{operation.Description.TrimEnd()}\n\n{block}";

            operation.Extensions["x-workspace-parameters"] = BuildExtension(attribute);
        }

        private static string FormatParameter(string parameterKey)
        {
            var definition = WorkspacePermissionParameters.TryGetDefinition(parameterKey);
            return definition is null
                ? $"`{parameterKey}`"
                : $"`{definition.Key}` ({definition.Label})";
        }

        private static Microsoft.OpenApi.Any.IOpenApiAny BuildExtension(RequiresWorkspaceParameterAttribute attribute)
        {
            var extension = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["membershipOnly"] = new Microsoft.OpenApi.Any.OpenApiBoolean(attribute.MembershipOnly),
                ["requireAll"] = new Microsoft.OpenApi.Any.OpenApiBoolean(attribute.RequireAll),
                ["editWorkspaceBypass"] = new Microsoft.OpenApi.Any.OpenApiBoolean(attribute.EditWorkspaceBypass),
            };

            if (attribute.Parameters.Length > 0)
            {
                var parameters = new Microsoft.OpenApi.Any.OpenApiArray();
                foreach (var parameter in attribute.Parameters)
                {
                    parameters.Add(new Microsoft.OpenApi.Any.OpenApiString(parameter));
                }

                extension["parameters"] = parameters;
            }

            if (!string.IsNullOrWhiteSpace(attribute.Notes))
            {
                extension["notes"] = new Microsoft.OpenApi.Any.OpenApiString(attribute.Notes.Trim());
            }

            return extension;
        }
    }
}
