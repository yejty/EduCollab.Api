using Drive.Api.Mapping;
using Drive.Api.Query;
using Drive.Api.Swagger;
using Space.Application.Services.Assets;
using Space.Application.Services.Flows;
using Space.Application.Services.Scenes;
using Space.Contracts.Responses.Assets;
using Space.Contracts.Responses.Flows;
using Space.Contracts.Responses.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.MinimalApis;

internal static class GroupContentEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.Groups.GetAssets, GetGroupAssets).AsApi("Groups_GetAssets", "Groups");
        routes.MapGet(ApiEndpoints.Groups.GetScenes, GetGroupScenes).AsApi("Groups_GetScenes", "Groups");
        routes.MapGet(ApiEndpoints.Groups.GetFlows, GetGroupFlows).AsApi("Groups_GetFlows", "Groups");
    }

    /// <summary>List assets placed in a group.</summary>
    /// <response code="200">Paged list of assets in the group.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadAssets", "addAssets", Notes = "Also requires effective access to the group.")]
    [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetGroupAssets(
        HttpContext httpContext,
        int groupId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (EndpointQuery.InvalidGroupId(httpContext, groupId) is { } invalid)
        {
            return invalid;
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.NamedResource.AllowedFields,
                ResourceSortProfiles.NamedResource.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        var assets = await assetService.GetAssetsInGroupAsync(groupId, cancellationToken);
        var sorted = ResourceSortProfiles.NamedResource.ApplyAssets(assets, sortSpecification);
        var response = PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse();
        foreach (var asset in response.Assets)
        {
            asset.CanManage = await assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
        }

        return ApiResults.Ok(response);
    }

    /// <summary>List scenes placed in a group.</summary>
    /// <response code="200">Paged list of scenes in the group.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadScenes", "addScenes", Notes = "Also requires effective access to the group.")]
    [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetGroupScenes(
        HttpContext httpContext,
        int groupId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (EndpointQuery.InvalidGroupId(httpContext, groupId) is { } invalid)
        {
            return invalid;
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.NamedResource.AllowedFields,
                ResourceSortProfiles.NamedResource.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        var scenes = await sceneService.GetScenesInGroupAsync(groupId, cancellationToken);
        var sorted = ResourceSortProfiles.NamedResource.ApplyScenes(scenes, sortSpecification);
        var response = PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse();
        foreach (var scene in response.Scenes)
        {
            scene.CanEdit = await sceneService.CanCurrentUserManageSceneAsync(scene.OwnerUserId, cancellationToken);
            scene.CanManage = scene.CanEdit;
        }

        return ApiResults.Ok(response);
    }

    /// <summary>List flows placed in a group.</summary>
    /// <response code="200">Paged list of flows in the group.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadFlows", "addFlows", Notes = "Also requires effective access to the group.")]
    [ProducesResponseType(typeof(FlowsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetGroupFlows(
        HttpContext httpContext,
        int groupId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IFlowService flowService,
        CancellationToken cancellationToken)
    {
        if (EndpointQuery.InvalidGroupId(httpContext, groupId) is { } invalid)
        {
            return invalid;
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.NamedResource.AllowedFields,
                ResourceSortProfiles.NamedResource.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        var flows = await flowService.GetFlowsInGroupAsync(groupId, cancellationToken);
        var sorted = ResourceSortProfiles.NamedResource.ApplyFlows(flows, sortSpecification);
        var response = PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse();
        foreach (var flow in response.Flows)
        {
            flow.CanManage = await flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);
        }

        return ApiResults.Ok(response);
    }
}
