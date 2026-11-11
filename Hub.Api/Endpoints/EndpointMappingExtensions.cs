namespace Hub.Api.MinimalApis;

internal static class EndpointMappingExtensions
{
    public static WebApplication MapHubApi(this WebApplication app)
    {
        var routes = app.MapGroup(string.Empty);
        routes.AddEndpointFilter<RequestValidationEndpointFilter>();

        HealthEndpoints.Map(routes);
        UserEndpoints.Map(routes);
        AdminWorkspaceEndpoints.Map(routes);
        AdminWorkspaceCreationRequestEndpoints.Map(routes);
        WorkspaceEndpoints.Map(routes);
        WorkspaceCreationReviewEndpoints.Map(routes);
        GroupEndpoints.Map(routes);

        return app;
    }

    public static RouteHandlerBuilder AsApi(this RouteHandlerBuilder builder, string name, string tag) =>
        builder.WithName(name).WithTags(tag);
}
