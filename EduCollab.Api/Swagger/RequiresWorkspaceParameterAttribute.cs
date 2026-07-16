namespace EduCollab.Api.Swagger
{
    /// <summary>
    /// Documents workspace permission parameter requirements for OpenAPI/Swagger.
    /// Does not enforce authorization at runtime; service-layer checks remain authoritative.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequiresWorkspaceParameterAttribute : Attribute
    {
        public RequiresWorkspaceParameterAttribute(params string[] parameters)
        {
            Parameters = parameters ?? Array.Empty<string>();
        }

        /// <summary>Parameter keys from <c>GET /api/workspace/permission-parameters</c>.</summary>
        public string[] Parameters { get; }

        /// <summary>When true, the caller must have every listed parameter. Default: any one parameter is sufficient.</summary>
        public bool RequireAll { get; init; }

        /// <summary>Endpoint only requires active workspace membership, not a specific parameter.</summary>
        public bool MembershipOnly { get; init; }

        /// <summary>Additional authorization context shown in the API docs.</summary>
        public string? Notes { get; init; }

        /// <summary>Document that <c>editWorkspace</c> grants owner-level access that bypasses normal parameter checks.</summary>
        public bool EditWorkspaceBypass { get; init; } = true;
    }
}
