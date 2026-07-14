namespace EduCollab.Api.Swagger
{
    /// <summary>
    /// Documents workspace permission preset requirements for OpenAPI/Swagger.
    /// Does not enforce authorization at runtime; service-layer checks remain authoritative.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequiresWorkspacePresetAttribute : Attribute
    {
        public RequiresWorkspacePresetAttribute(params string[] presets)
        {
            Presets = presets ?? Array.Empty<string>();
        }

        /// <summary>Preset keys from <c>GET /api/workspace/permission-presets</c>.</summary>
        public string[] Presets { get; }

        /// <summary>When true, the caller must have every listed preset. Default: any one preset is sufficient.</summary>
        public bool RequireAll { get; init; }

        /// <summary>Endpoint only requires active workspace membership, not a specific preset.</summary>
        public bool MembershipOnly { get; init; }

        /// <summary>Additional authorization context shown in the API docs.</summary>
        public string? Notes { get; init; }

        /// <summary>Document that <c>editWorkspace</c> grants owner-level access that bypasses normal preset checks.</summary>
        public bool EditWorkspaceBypass { get; init; } = true;
    }
}
