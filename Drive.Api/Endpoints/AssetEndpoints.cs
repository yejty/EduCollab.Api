using Drive.Api.Mapping;
using Drive.Api.Query;
using Drive.Api.Requests.Assets;
using Drive.Api.Swagger;
using Space.Application.Models;
using Space.Application.Services.Assets;
using Space.Application.Services.Content;
using Space.Contracts.Requests.Assets;
using Space.Contracts.Responses.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.MinimalApis;

internal static class AssetEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.Assets.Create, CreateAsset)
            .AsApi("Assets_Create", "Assets")
            .DisableAntiforgery()
            .Accepts<CreateAssetFormRequest>("multipart/form-data");
        routes.MapGet(ApiEndpoints.Assets.GetAll, GetAssets).AsApi("Assets_GetAll", "Assets");
        routes.MapGet(ApiEndpoints.Assets.Get, GetAsset).AsApi("Assets_Get", "Assets");
        routes.MapPut(ApiEndpoints.Assets.Update, UpdateAsset).AsApi("Assets_Update", "Assets");
        routes.MapDelete(ApiEndpoints.Assets.Delete, DeleteAsset).AsApi("Assets_Delete", "Assets");
        routes.MapGet(ApiEndpoints.Assets.Content, GetAssetContent).AsApi("Assets_GetContent", "Assets");
        routes.MapPut(ApiEndpoints.Assets.Content, PutAssetContent)
            .AsApi("Assets_PutContent", "Assets")
            .DisableAntiforgery();
    }

    /// <summary>Create a new asset with required ZIP content.</summary>
    /// <response code="201">Asset was created with content.</response>
    /// <response code="400">Metadata or ZIP file is invalid.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> CreateAsset(
        HttpContext httpContext,
        [FromForm] CreateAssetFormRequest request,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_content", "A non-empty ZIP file is required.");
        }

        if (!AssetContentFormats.IsZipContent(request.File.ContentType, request.File.FileName))
        {
            return ApiResults.BadRequest(httpContext, "invalid_content", "Asset content must be a ZIP file.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResults.BadRequest(httpContext, "invalid_name", "Name is required.");
        }

        var asset = new Asset
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            AssetType = AssetContentFormats.DefaultAssetType,
        };

        await using var stream = request.File.OpenReadStream();
        var created = await assetService.CreateAssetWithContentAsync(
            asset,
            ResourceGroupPlacement.ResolveGroupIds(request.GroupIds),
            request.File.ContentType,
            request.File.FileName,
            stream,
            cancellationToken);
        if (!created)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "Asset could not be created.");
        }

        var response = asset.MapToResponse();
        response.CanManage = await assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
        return ApiResults.Created(ApiRoutes.For(ApiEndpoints.Assets.Get, new { assetId = asset.Id }), response);
    }

    /// <summary>List accessible assets in the current workspace.</summary>
    /// <response code="200">Paged list of assets.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadAssets", "addAssets", Notes = "Also requires effective access to each asset (ownership or group membership).")]
    [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetAssets(
        HttpContext httpContext,
        [FromQuery] string? owner,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (!OwnerQueryParser.TryParse(owner, out var ownerIsCurrentUser, out var ownerError))
        {
            return ApiResults.BadRequest(httpContext, "invalid_filter", ownerError!);
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

        var assets = ownerIsCurrentUser
            ? await assetService.GetMyAssetsAsync(cancellationToken)
            : await assetService.GetAllAssetsAsync(cancellationToken);
        var sorted = ResourceSortProfiles.NamedResource.ApplyAssets(assets, sortSpecification);
        var response = PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse();
        foreach (var asset in response.Assets)
        {
            asset.CanManage = await assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
        }

        return ApiResults.Ok(response);
    }

    /// <summary>Retrieve an asset by id.</summary>
    /// <response code="200">Returns the asset.</response>
    /// <response code="404">Asset was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadAssets", "addAssets", Notes = "Also requires effective access to the asset (ownership or group membership).")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetAsset(
        HttpContext httpContext,
        int assetId,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        var asset = await assetService.GetAssetByIdAsync(assetId, cancellationToken);
        if (asset is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var response = asset.MapToResponse();
        response.CanManage = await assetService.CanCurrentUserManageAssetAsync(asset.OwnerUserId, cancellationToken);
        return ApiResults.Ok(response);
    }

    /// <summary>Update asset metadata.</summary>
    /// <response code="200">Asset was updated.</response>
    /// <response code="404">Asset was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the asset.")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> UpdateAsset(
        HttpContext httpContext,
        int assetId,
        UpdateAssetRequest request,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        var updated = await assetService.UpdateAssetAsync(request.MapToAsset(assetId), cancellationToken);
        if (updated is null)
        {
            return ApiResults.NotFound(httpContext, "update_failed", "Asset was not found.");
        }

        var response = updated.MapToResponse();
        response.CanManage = await assetService.CanCurrentUserManageAssetAsync(updated.OwnerUserId, cancellationToken);
        return ApiResults.Ok(response);
    }

    /// <summary>Delete an asset.</summary>
    /// <response code="204">Asset was deleted.</response>
    /// <response code="404">Asset was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the asset.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> DeleteAsset(
        HttpContext httpContext,
        int assetId,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        var deleted = await assetService.DeleteAssetAsync(assetId, cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    /// <summary>Download asset binary content (ZIP file).</summary>
    /// <response code="200">Asset ZIP content.</response>
    /// <response code="404">Asset or content was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadAssets", "addAssets", Notes = "Also requires effective access to the asset (ownership or group membership).")]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetAssetContent(
        HttpContext httpContext,
        int assetId,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        var content = await assetService.GetAssetContentAsync(assetId, cancellationToken);
        if (content is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var contentType = string.IsNullOrWhiteSpace(content.ContentType)
            ? AssetContentFormats.ZipContentType
            : content.ContentType;
        return ApiResults.File(content.Data, contentType, $"asset-{assetId}.zip");
    }

    /// <summary>Upload or replace asset binary content (ZIP file).</summary>
    /// <response code="204">Content was saved.</response>
    /// <response code="400">File is missing, empty, or not a ZIP.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addAssets", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the asset.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> PutAssetContent(
        HttpContext httpContext,
        int assetId,
        IFormFile? file,
        IAssetService assetService,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_content", "A non-empty ZIP file is required.");
        }

        if (!AssetContentFormats.IsZipContent(file.ContentType, file.FileName))
        {
            return ApiResults.BadRequest(httpContext, "invalid_content", "Asset content must be a ZIP file.");
        }

        await using var stream = file.OpenReadStream();
        await assetService.SaveAssetContentAsync(assetId, file.ContentType, file.FileName, stream, cancellationToken);
        return ApiResults.NoContent();
    }
}
