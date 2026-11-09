using EduCollab.Api.Swagger;
using EduCollab.Application.Services.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.MinimalApis;

internal static class AssetGroupEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.AssetGroups.GetAll, GetAssetGroups).AsApi("AssetGroups_GetAll", "AssetGroups");
        routes.MapPost(ApiEndpoints.AssetGroups.Create, AddAssetGroup).AsApi("AssetGroups_Create", "AssetGroups");
        routes.MapPut(ApiEndpoints.AssetGroups.Update, SetAssetGroups).AsApi("AssetGroups_Update", "AssetGroups");
        routes.MapDelete(ApiEndpoints.AssetGroups.Delete, RemoveAssetGroup).AsApi("AssetGroups_Delete", "AssetGroups");
    }

    /// <summary>List groups an asset is shared with.</summary>
    /// <response code="200">Groups returned.</response>
    /// <response code="404">Asset was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires effective access to the asset.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetAssetGroups(
        HttpContext httpContext,
        [FromQuery] int assetId,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_asset_id", "assetId is required and must be a positive integer.");
        }

        try
        {
            var groupIds = await assetService.GetAssetGroupIdsAsync(assetId, cancellationToken);
            return ApiResults.Ok(new ResourceGroupsResponse { GroupIds = groupIds });
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Share an asset with a group.</summary>
    /// <response code="201">Asset was shared with the group.</response>
    /// <response code="404">Asset or group was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires manage access to the asset.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> AddAssetGroup(
        HttpContext httpContext,
        AttachAssetGroupRequest request,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (request.AssetId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_asset_id", "assetId must be a positive integer.");
        }

        if (request.GroupId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId must be a positive integer.");
        }

        var added = await assetService.AddAssetGroupAsync(request.AssetId, request.GroupId, cancellationToken);
        if (!added)
        {
            return ApiResults.NotFound(httpContext, "share_failed", "Asset or group was not found.");
        }

        var groupIds = await assetService.GetAssetGroupIdsAsync(request.AssetId, cancellationToken);
        return ApiResults.Status(StatusCodes.Status201Created, new ResourceGroupsResponse { GroupIds = groupIds });
    }

    /// <summary>Replace all group shares for an asset.</summary>
    /// <response code="200">Group shares were replaced.</response>
    /// <response code="404">Asset was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires manage access to the asset.")]
    [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> SetAssetGroups(
        HttpContext httpContext,
        [FromQuery] int assetId,
        SetResourceGroupsRequest request,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_asset_id", "assetId is required and must be a positive integer.");
        }

        try
        {
            var groupIds = await assetService.SetAssetGroupIdsAsync(assetId, request.GroupIds, cancellationToken);
            return groupIds is null
                ? ApiResults.NotFound(httpContext, "update_failed", "Asset was not found.")
                : ApiResults.Ok(new ResourceGroupsResponse { GroupIds = groupIds });
        }
        catch (ArgumentException ex) when (ex.ParamName == "groupIds")
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", ex.Message);
        }
    }

    /// <summary>Remove an asset from a group.</summary>
    /// <response code="204">Asset was removed from the group.</response>
    /// <response code="404">Asset or group was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires manage access to the asset.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> RemoveAssetGroup(
        HttpContext httpContext,
        [FromQuery] int assetId,
        [FromQuery] int groupId,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_asset_id", "assetId is required and must be a positive integer.");
        }

        if (groupId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_group_id", "groupId is required and must be a positive integer.");
        }

        var removed = await assetService.RemoveAssetGroupAsync(assetId, groupId, cancellationToken);
        return removed ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }
}
