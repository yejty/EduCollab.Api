using EduCollab.Application.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EduCollab.Api.Swagger
{
    public sealed class RequiresWorkspacePresetOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo?
                .GetCustomAttributes(typeof(RequiresWorkspacePresetAttribute), inherit: true)
                .OfType<RequiresWorkspacePresetAttribute>()
                .FirstOrDefault();

            if (attribute is null)
                return;

            var lines = new List<string>();
            if (attribute.MembershipOnly)
            {
                lines.Add("**Workspace presets:** active workspace membership (any member).");
            }
            else if (attribute.Presets.Length > 0)
            {
                var presetLabels = attribute.Presets
                    .Select(FormatPreset)
                    .ToList();

                var joiner = attribute.RequireAll ? " and " : " or ";
                lines.Add($"**Workspace presets:** {string.Join(joiner, presetLabels)}.");
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

            operation.Extensions["x-workspace-presets"] = BuildExtension(attribute);
        }

        private static string FormatPreset(string presetKey)
        {
            var definition = WorkspacePermissionPresets.TryGetDefinition(presetKey);
            return definition is null
                ? $"`{presetKey}`"
                : $"`{definition.Key}` ({definition.Label})";
        }

        private static Microsoft.OpenApi.Any.IOpenApiAny BuildExtension(RequiresWorkspacePresetAttribute attribute)
        {
            var extension = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["membershipOnly"] = new Microsoft.OpenApi.Any.OpenApiBoolean(attribute.MembershipOnly),
                ["requireAll"] = new Microsoft.OpenApi.Any.OpenApiBoolean(attribute.RequireAll),
                ["editWorkspaceBypass"] = new Microsoft.OpenApi.Any.OpenApiBoolean(attribute.EditWorkspaceBypass),
            };

            if (attribute.Presets.Length > 0)
            {
                var presets = new Microsoft.OpenApi.Any.OpenApiArray();
                foreach (var preset in attribute.Presets)
                {
                    presets.Add(new Microsoft.OpenApi.Any.OpenApiString(preset));
                }

                extension["presets"] = presets;
            }

            if (!string.IsNullOrWhiteSpace(attribute.Notes))
            {
                extension["notes"] = new Microsoft.OpenApi.Any.OpenApiString(attribute.Notes.Trim());
            }

            return extension;
        }
    }
}
