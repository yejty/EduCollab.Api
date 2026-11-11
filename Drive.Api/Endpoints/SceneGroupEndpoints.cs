using Drive.Api.Swagger;
using Space.Application.Models;
using Space.Application.Services.Scenes;
using Space.Contracts.Requests.Groups;
using Space.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.MinimalApis;

internal static class SceneGroupEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.SceneGroups.GetAll, GetSceneGroups).AsApi("SceneGroups_GetAll", "SceneGroups");
        routes.MapPost(ApiEndpoints.SceneGroups.Create, AddSceneGroup).AsApi("SceneGroups_Create", "SceneGroups");
        routes.MapPut(ApiEndpoints.SceneGroups.Update, SetSceneGroups).AsApi("SceneGroups_Update", "SceneGroups");
        routes.MapDelete(ApiEndpoints.SceneGroups.Delete, RemoveSceneGroup).AsApi("SceneGroups_Delete", "SceneGroups");
    }

    /// <summary>List groups a scene is shared with.</summary>
    /// <response code="200">Groups returned.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes", Notes = "Also requires effective access to the scene.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetSceneGroups(
        HttpContext httpContext,
        [FromQuery] int sceneId,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        try
        {
            var shares = await sceneService.GetSceneGroupSharesAsync(sceneId, cancellationToken);
            return ApiResults.Ok(MapShares(shares));
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Share a scene with a group.</summary>
    /// <response code="201">Scene was shared.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes", Notes = "Also requires manage access to the scene.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> AddSceneGroup(
        HttpContext httpContext,
        AttachSceneGroupRequest request,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (request.SceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId must be a positive integer.");
        }

        if (request.GroupId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId must be a positive integer.");
        }

        var added = await sceneService.AddSceneGroupAsync(request.SceneId, request.GroupId, request.IncludeAssets, cancellationToken);
        if (!added)
        {
            return ApiResults.NotFound(httpContext, "share_failed", "Scene or group was not found.");
        }

        var shares = await sceneService.GetSceneGroupSharesAsync(request.SceneId, cancellationToken);
        return ApiResults.Status(StatusCodes.Status201Created, MapShares(shares));
    }

    /// <summary>Replace all group shares for a scene.</summary>
    /// <response code="200">Group shares were replaced.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes", Notes = "Also requires manage access to the scene.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> SetSceneGroups(
        HttpContext httpContext,
        [FromQuery] int sceneId,
        SetSceneGroupsRequest request,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        try
        {
            var shares = (request.Groups ?? [])
                .Select(group => new SceneGroupShare
                {
                    GroupId = group.GroupId,
                    IncludeAssets = group.IncludeAssets,
                })
                .ToList();
            var updated = await sceneService.SetSceneGroupSharesAsync(sceneId, shares, cancellationToken);
            return updated is null
                ? ApiResults.NotFound(httpContext, "update_failed", "Scene was not found.")
                : ApiResults.Ok(MapShares(updated));
        }
        catch (ArgumentException ex) when (ex.ParamName == "groupIds")
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", ex.Message);
        }
    }

    /// <summary>Remove a scene from a group.</summary>
    /// <response code="204">Scene was removed from the group.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes", Notes = "Also requires manage access to the scene.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> RemoveSceneGroup(
        HttpContext httpContext,
        [FromQuery] int sceneId,
        [FromQuery] int groupId,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        if (groupId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId is required and must be a positive integer.");
        }

        var removed = await sceneService.RemoveSceneGroupAsync(sceneId, groupId, cancellationToken);
        return removed ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    private static ResourceGroupsResponse MapShares(IReadOnlyList<SceneGroupShare> shares) =>
        new()
        {
            GroupIds = shares.Select(share => share.GroupId).ToList(),
            Groups = shares
                .Select(share => new ResourceGroupShareResponse
                {
                    GroupId = share.GroupId,
                    IncludeAssets = share.IncludeAssets,
                })
                .ToList(),
        };
}
