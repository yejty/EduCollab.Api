namespace EduCollab.Api.MinimalApis;

internal static class EndpointMappingExtensions
{
    public static WebApplication MapEduCollabApi(this WebApplication app)
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
        AssetEndpoints.Map(routes);
        AssetGroupEndpoints.Map(routes);
        SceneEndpoints.Map(routes);
        SceneGroupEndpoints.Map(routes);
        SceneAssetEndpoints.Map(routes);
        FlowEndpoints.Map(routes);
        FlowGroupEndpoints.Map(routes);
        FlowSceneEndpoints.Map(routes);
        SessionEndpoints.Map(routes);
        SessionAssetEndpoints.Map(routes);
        SessionSceneEndpoints.Map(routes);
        PublicSessionEndpoints.Map(routes);
        PublicSessionContentEndpoints.Map(routes);
        InternalSessionEndpoints.Map(routes);

        return app;
    }

    public static RouteHandlerBuilder AsApi(this RouteHandlerBuilder builder, string name, string tag) =>
        builder.WithName(name).WithTags(tag);
}
