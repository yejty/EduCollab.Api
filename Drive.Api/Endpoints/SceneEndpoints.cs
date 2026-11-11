using System.Text.Json;
using Drive.Api.Mapping;
using Drive.Api.Query;
using Drive.Api.Requests.Scenes;
using Drive.Api.Swagger;
using Space.Application.Services.Content;
using Space.Application.Services.Scenes;
using Space.Contracts.Requests.Scenes;
using Space.Contracts.Responses.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Drive.Api.MinimalApis;

internal static class SceneEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.Scenes.Create, CreateScene)
            .AsApi("Scenes_Create", "Scenes")
            .DisableAntiforgery()
            .Accepts<CreateSceneRequest>("application/json");
        routes.MapGet(ApiEndpoints.Scenes.GetAll, GetScenes).AsApi("Scenes_GetAll", "Scenes");
        routes.MapGet(ApiEndpoints.Scenes.Get, GetScene).AsApi("Scenes_Get", "Scenes");
        routes.MapPut(ApiEndpoints.Scenes.Update, UpdateScene)
            .AsApi("Scenes_Update", "Scenes")
            .DisableAntiforgery()
            .Accepts<UpdateSceneRequest>("application/json");
        routes.MapDelete(ApiEndpoints.Scenes.Delete, DeleteScene).AsApi("Scenes_Delete", "Scenes");
    }

    /// <summary>Create a new scene from JSON or a multipart JSON file.</summary>
    /// <response code="201">Scene was created.</response>
    /// <response code="400">Scene could not be created or references an invalid asset.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes")]
    [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> CreateScene(
        HttpContext httpContext,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        CreateSceneRequest request;
        if (httpContext.Request.HasFormContentType)
        {
            var formRequest = await ReadCreateFormAsync(httpContext, cancellationToken);
            if (formRequest.Error is not null)
            {
                return formRequest.Error;
            }

            request = formRequest.Request!;
        }
        else
        {
            var jsonRequest = await ReadJsonAsync<CreateSceneRequest>(httpContext, cancellationToken);
            if (jsonRequest.Error is not null)
            {
                return jsonRequest.Error;
            }

            request = jsonRequest.Request!;
        }

        var scene = request.MapToScene();
        var created = await sceneService.CreateSceneAsync(
            scene,
            ResourceGroupPlacement.ResolveGroupIds(request.GroupIds),
            cancellationToken);
        if (!created)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "Scene could not be created.");
        }

        var response = scene.MapToResponse();
        response.CanEdit = await sceneService.CanCurrentUserManageSceneAsync(response.OwnerUserId, cancellationToken);
        response.CanManage = response.CanEdit;
        return ApiResults.Created(ApiRoutes.For(ApiEndpoints.Scenes.Get, new { sceneId = scene.Id }), response);
    }

    /// <summary>List accessible scenes in the current workspace.</summary>
    /// <response code="200">Paged list of scenes.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadScenes", "addScenes", Notes = "Also requires effective access to each scene (ownership or group membership).")]
    [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetScenes(
        HttpContext httpContext,
        [FromQuery] string? owner,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ISceneService sceneService,
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

        var scenes = ownerIsCurrentUser
            ? await sceneService.GetMyScenesAsync(cancellationToken)
            : await sceneService.GetAllScenesAsync(cancellationToken);
        var sorted = ResourceSortProfiles.NamedResource.ApplyScenes(scenes, sortSpecification);
        var response = PaginationApplier.Apply(sorted, paginationSpecification).MapToResponse();
        foreach (var scene in response.Scenes)
        {
            scene.CanEdit = await sceneService.CanCurrentUserManageSceneAsync(scene.OwnerUserId, cancellationToken);
            scene.CanManage = scene.CanEdit;
        }

        return ApiResults.Ok(response);
    }

    /// <summary>Retrieve a scene by id.</summary>
    /// <response code="200">Returns the scene.</response>
    /// <response code="404">Scene was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("loadScenes", "addScenes", Notes = "Also requires effective access to the scene (ownership or group membership).")]
    [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetScene(
        HttpContext httpContext,
        int sceneId,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        var scene = await sceneService.GetSceneByIdAsync(sceneId, cancellationToken);
        if (scene is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var response = scene.MapToResponse();
        response.CanEdit = await sceneService.CanCurrentUserManageSceneAsync(response.OwnerUserId, cancellationToken);
        response.CanManage = response.CanEdit;
        return ApiResults.Ok(response);
    }

    /// <summary>Update scene metadata and JSON content from JSON or a multipart JSON file.</summary>
    /// <response code="200">Scene was updated.</response>
    /// <response code="404">Scene was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the scene.")]
    [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> UpdateScene(
        HttpContext httpContext,
        int sceneId,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        UpdateSceneRequest request;
        if (httpContext.Request.HasFormContentType)
        {
            var formRequest = await ReadUpdateFormAsync(httpContext, cancellationToken);
            if (formRequest.Error is not null)
            {
                return formRequest.Error;
            }

            request = formRequest.Request!;
        }
        else
        {
            var jsonRequest = await ReadJsonAsync<UpdateSceneRequest>(httpContext, cancellationToken);
            if (jsonRequest.Error is not null)
            {
                return jsonRequest.Error;
            }

            request = jsonRequest.Request!;
        }

        var updated = await sceneService.UpdateSceneAsync(request.MapToScene(sceneId), cancellationToken);
        if (updated is null)
        {
            return ApiResults.NotFound(httpContext, "update_failed", "Scene was not found.");
        }

        var response = updated.MapToResponse();
        response.CanEdit = await sceneService.CanCurrentUserManageSceneAsync(response.OwnerUserId, cancellationToken);
        response.CanManage = response.CanEdit;
        return ApiResults.Ok(response);
    }

    /// <summary>Delete a scene.</summary>
    /// <response code="204">Scene was deleted.</response>
    /// <response code="404">Scene was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addScenes", Notes = "Also requires ownership, editWorkspace, or addGroups-based manage access to the scene.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> DeleteScene(
        HttpContext httpContext,
        int sceneId,
        ISceneService sceneService,
        CancellationToken cancellationToken)
    {
        var deleted = await sceneService.DeleteSceneAsync(sceneId, cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    private static async Task<(CreateSceneRequest? Request, IResult? Error)> ReadCreateFormAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, ApiResults.BadRequest(httpContext, "invalid_name", "Name is required."));
        }

        string jsonContent;
        try
        {
            jsonContent = SceneFormContentResolver.ResolveJsonContent(FormOrNull(form, "jsonContent"), form.Files.GetFile("jsonFile"));
            SceneFormContentResolver.ParseJsonContent(jsonContent);
        }
        catch (ArgumentException ex)
        {
            return (null, ApiResults.BadRequest(httpContext, "invalid_json_content", ex.Message));
        }

        return (new CreateSceneRequest
        {
            Name = name.Trim(),
            Description = FormOrNull(form, "description"),
            GroupIds = ReadGroupIds(form),
            JsonContent = SceneFormContentResolver.ParseJsonContent(jsonContent),
        }, null);
    }

    private static async Task<(UpdateSceneRequest? Request, IResult? Error)> ReadUpdateFormAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, ApiResults.BadRequest(httpContext, "invalid_name", "Name is required."));
        }

        string jsonContent;
        try
        {
            jsonContent = SceneFormContentResolver.ResolveJsonContent(FormOrNull(form, "jsonContent"), form.Files.GetFile("jsonFile"));
            SceneFormContentResolver.ParseJsonContent(jsonContent);
        }
        catch (ArgumentException ex)
        {
            return (null, ApiResults.BadRequest(httpContext, "invalid_json_content", ex.Message));
        }

        return (new UpdateSceneRequest
        {
            Name = name.Trim(),
            Description = FormOrNull(form, "description"),
            JsonContent = SceneFormContentResolver.ParseJsonContent(jsonContent),
        }, null);
    }

    private static async Task<(T? Request, IResult? Error)> ReadJsonAsync<T>(
        HttpContext httpContext,
        CancellationToken cancellationToken)
        where T : class
    {
        T? request;
        try
        {
            request = await httpContext.Request.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new BadHttpRequestException("The request body contains invalid JSON.", ex);
        }

        if (request is null)
        {
            var empty = new ModelStateDictionary();
            empty.AddModelError(string.Empty, "A non-empty request body is required.");
            return (null, ApiResults.Validation(httpContext, empty));
        }

        var modelState = new ModelStateDictionary();
        if (!RequestValidation.TryValidate(request, modelState))
        {
            return (null, ApiResults.Validation(httpContext, modelState));
        }

        return (request, null);
    }

    private static string? FormOrNull(IFormCollection form, string name)
    {
        if (!form.ContainsKey(name))
        {
            return null;
        }

        var value = form[name].ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static List<int>? ReadGroupIds(IFormCollection form)
    {
        var values = new List<string>();
        if (form.ContainsKey("groupIds"))
        {
            values.AddRange(form["groupIds"]);
        }

        foreach (var key in form.Keys)
        {
            if (key.StartsWith("groupIds[", StringComparison.Ordinal))
            {
                values.Add(form[key].ToString());
            }
        }

        if (values.Count == 0)
        {
            return null;
        }

        var ids = new List<int>();
        foreach (var value in values)
        {
            if (int.TryParse(value, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
