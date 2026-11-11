using Hub.Api.Mapping;
using Hub.Api.Query;
using Hub.Api.Swagger;
using Space.Application.Models;
using Space.Application.Services.Workspaces;
using Space.Contracts.Requests.Users;
using Space.Contracts.Requests.Workspaces;
using Space.Contracts.Responses;
using Space.Contracts.Responses.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Api.MinimalApis;

internal static class WorkspaceEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.Workspace.PermissionParameters, GetPermissionParameters)
            .AsApi("Workspaces_GetPermissionParameters", "Workspaces");
        routes.MapPost(ApiEndpoints.Workspace.Invite, InviteToWorkspace)
            .AsApi("Workspaces_Invite", "Workspaces");
        routes.MapPost(ApiEndpoints.WorkspaceInvitations.Accept, CreateWorkspaceUser)
            .AsApi("Workspaces_AcceptInvitation", "Workspaces");
        routes.MapPost(ApiEndpoints.WorkspaceInvitations.Join, JoinWorkspaceFromInvitation)
            .AsApi("Workspaces_JoinInvitation", "Workspaces");
        routes.MapGet(ApiEndpoints.Workspace.GetMember, GetWorkspaceUser)
            .AsApi("Workspaces_GetMember", "Workspaces");
        routes.MapPost(ApiEndpoints.Workspace.RequestCreation, RequestWorkspaceCreation)
            .AsApi("Workspaces_RequestCreation", "Workspaces");
        routes.MapGet(ApiEndpoints.Workspace.GetMyCreationRequest, GetMyWorkspaceCreationRequest)
            .AsApi("Workspaces_GetMyCreationRequest", "Workspaces");
        routes.MapPost(ApiEndpoints.Workspace.Create, CreateWorkspace)
            .AsApi("Workspaces_Create", "Workspaces");
        routes.MapGet(ApiEndpoints.Workspace.Get, GetCurrentWorkspace)
            .AsApi("Workspaces_GetCurrent", "Workspaces");
        routes.MapGet(ApiEndpoints.Workspace.GetAllMembers, GetCurrentWorkspaceMembers)
            .AsApi("Workspaces_GetMembers", "Workspaces");
        routes.MapGet(ApiEndpoints.Workspace.Thumbnail, GetWorkspaceThumbnail)
            .AsApi("Workspaces_GetThumbnail", "Workspaces");
        routes.MapPut(ApiEndpoints.Workspace.Thumbnail, PutWorkspaceThumbnail)
            .AsApi("Workspaces_PutThumbnail", "Workspaces")
            .DisableAntiforgery();
        routes.MapDelete(ApiEndpoints.Workspace.Thumbnail, DeleteWorkspaceThumbnail)
            .AsApi("Workspaces_DeleteThumbnail", "Workspaces");
        routes.MapPut(ApiEndpoints.Workspace.Update, UpdateWorkspace)
            .AsApi("Workspaces_Update", "Workspaces");
        routes.MapDelete(ApiEndpoints.Workspace.Delete, DeleteWorkspace)
            .AsApi("Workspaces_Delete", "Workspaces");
        routes.MapDelete(ApiEndpoints.Workspace.DeleteMember, DeleteWorkspaceMember)
            .AsApi("Workspaces_DeleteMember", "Workspaces");
        routes.MapPut(ApiEndpoints.Workspace.UpdateMember, UpdateWorkspaceMember)
            .AsApi("Workspaces_UpdateMember", "Workspaces");
    }

    /// <summary>Lists all workspace permission parameter keys and named presets.</summary>
    /// <response code="200">Parameter catalog and presets returned.</response>
    [Authorize]
    [RequiresWorkspaceParameter(MembershipOnly = true)]
    [ProducesResponseType(typeof(WorkspacePermissionParametersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static IResult GetPermissionParameters() => ApiResults.Ok(ContractMapping.MapCatalogToResponse());

    /// <summary>Invite a user into the current workspace.</summary>
    /// <response code="200">User invited successfully.</response>
    /// <response code="400">Invalid invitation attempt.</response>
    [Authorize]
    [RequiresWorkspaceParameter("inviteUsers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> InviteToWorkspace(
        HttpContext httpContext,
        InviteUserRequest inviteUserRequest,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!WorkspacePermissionParameters.TryNormalizeKeys(inviteUserRequest.Parameters, out var parameters, out var error))
        {
            return ApiResults.BadRequest(httpContext, "invalid_parameter", error ?? "Invalid parameter keys.");
        }

        await workspaceService.InviteUserToCurrentWorkspaceAsync(
            inviteUserRequest.Email,
            parameters,
            inviteUserRequest.GroupId,
            cancellationToken);
        return ApiResults.Ok();
    }

    /// <summary>Creates a new user in the workspace based on the provided token.</summary>
    /// <response code="201">User created.</response>
    /// <response code="400">Invalid invitation attempt.</response>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> CreateWorkspaceUser(
        HttpContext httpContext,
        RegisterUserRequest request,
        string invitationToken,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var user = request.MapToUser();
        var created = await workspaceService.CreateUserFromInvitationAsync(user, request.Password, invitationToken, cancellationToken);
        if (!created)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "The invitation is invalid, expired, or the user could not be created.");
        }

        var member = await workspaceService.GetWorkspaceMemberAsync(user.WorkspaceId!.Value, user.Id, cancellationToken);
        if (member is null)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "User was created but workspace membership could not be loaded.");
        }

        return ApiResults.Status(StatusCodes.Status201Created, member.MapToResponse());
    }

    /// <summary>Joins the current user to a workspace using an invitation token.</summary>
    /// <response code="200">User joined the workspace.</response>
    /// <response code="400">Invalid invitation attempt.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> JoinWorkspaceFromInvitation(
        HttpContext httpContext,
        string invitationToken,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var member = await workspaceService.JoinWorkspaceFromInvitationAsync(invitationToken, cancellationToken);
        return member is null
            ? ApiResults.BadRequest(httpContext, "join_failed", "The invitation is invalid, expired, or could not be accepted.")
            : ApiResults.Ok(member.MapToResponse());
    }

    /// <summary>Get one workspace member.</summary>
    /// <response code="200">Returns the workspace member.</response>
    /// <response code="403">Caller cannot view workspace members.</response>
    /// <response code="404">Workspace or member was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("seeUsersTab", "inviteUsers")]
    [ProducesResponseType(typeof(WorkspaceMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetWorkspaceUser(
        HttpContext httpContext,
        int userId,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var myMembership = await workspaceService.GetCurrentUserWorkspaceMemberAsync(cancellationToken);
        if (myMembership is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        if (!WorkspaceParameterPermissions.CanSeeUsersTab(myMembership))
        {
            return ApiResults.Forbidden(httpContext, "forbidden", "You do not have permission to view workspace members.");
        }

        var member = await workspaceService.GetCurrentWorkspaceMemberAsync(userId, cancellationToken);
        return member is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(member.MapToResponse());
    }

    /// <summary>Submit a workspace creation request for platform admin approval.</summary>
    /// <response code="201">Creation request was submitted.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> RequestWorkspaceCreation(
        HttpContext httpContext,
        RequestWorkspaceCreationRequest request,
        IWorkspaceCreationRequestService workspaceCreationRequestService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await workspaceCreationRequestService.SubmitRequestAsync(
                request.Name,
                request.Description,
                request.Type,
                cancellationToken);
            return ApiResults.Status(StatusCodes.Status201Created, created.MapToResponse());
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "request_failed", ex.Message);
        }
    }

    /// <summary>Get the current user's latest workspace creation request.</summary>
    /// <response code="200">Returns the latest creation request.</response>
    /// <response code="404">No creation request was found.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceCreationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetMyWorkspaceCreationRequest(
        HttpContext httpContext,
        IWorkspaceCreationRequestService workspaceCreationRequestService,
        CancellationToken cancellationToken)
    {
        var request = await workspaceCreationRequestService.GetCurrentUserLatestRequestAsync(cancellationToken);
        return request is null ? ApiResults.NotFound(httpContext) : ApiResults.Ok(request.MapToResponse());
    }

    /// <summary>Create a new workspace and assign the current user as the owner.</summary>
    /// <response code="201">Workspace was created successfully.</response>
    /// <response code="400">Workspace could not be created.</response>
    [Authorize]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public static async Task<IResult> CreateWorkspace(
        HttpContext httpContext,
        CreateWorkspaceRequest request,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var workspace = request.MapToWorkspace();
        try
        {
            var created = await workspaceService.CreateWorkspaceAsync(workspace, request.ApprovalToken, cancellationToken);
            if (!created)
            {
                return ApiResults.BadRequest(httpContext, "creation_failed", "Workspace could not be created.");
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("approval token", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResults.BadRequest(httpContext, "invalid_approval_token", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", ex.Message);
        }

        var response = workspace.MapToResponse();
        response.CurrentUserRole = WorkspaceRole.Owner.ToString();
        return ApiResults.Created(ApiRoutes.For(ApiEndpoints.Workspace.Get), response);
    }

    /// <summary>Retrieve the current user's workspace.</summary>
    /// <response code="200">Returns the active workspace.</response>
    /// <response code="404">Active workspace was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter(MembershipOnly = true)]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetCurrentWorkspace(
        HttpContext httpContext,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaceService.GetCurrentWorkspaceAsync(cancellationToken);
        if (workspace is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var response = workspace.MapToResponse();
        var myMembership = await workspaceService.GetCurrentUserWorkspaceMemberAsync(cancellationToken);
        response.CurrentUserRole = myMembership?.Role.ToString();
        return ApiResults.Ok(response);
    }

    /// <summary>List members of the current workspace.</summary>
    /// <response code="200">Paged list of workspace members.</response>
    /// <response code="403">Caller cannot list workspace members.</response>
    /// <response code="404">Workspace was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("seeUsersTab", "inviteUsers")]
    [ProducesResponseType(typeof(WorkspaceMembersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetCurrentWorkspaceMembers(
        HttpContext httpContext,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaceService.GetCurrentWorkspaceAsync(cancellationToken);
        if (workspace is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var myMembership = await workspaceService.GetCurrentUserWorkspaceMemberAsync(cancellationToken);
        if (myMembership is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        if (!WorkspaceParameterPermissions.CanSeeUsersTab(myMembership))
        {
            return ApiResults.Forbidden(httpContext, "forbidden", "You do not have permission to list workspace members.");
        }

        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.WorkspaceMember.AllowedFields,
                ResourceSortProfiles.WorkspaceMember.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        var sortedMembers = ResourceSortProfiles.WorkspaceMember.Apply(
            await workspaceService.GetCurrentWorkspaceMembersAsync(cancellationToken),
            sortSpecification);
        return ApiResults.Ok(PaginationApplier.Apply(sortedMembers, paginationSpecification).MapToResponse());
    }

    /// <summary>Retrieve the current workspace thumbnail image.</summary>
    /// <response code="200">Thumbnail image bytes.</response>
    /// <response code="404">Workspace or thumbnail was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter(MembershipOnly = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetWorkspaceThumbnail(
        HttpContext httpContext,
        IWorkspaceThumbnailService workspaceThumbnailService,
        CancellationToken cancellationToken)
    {
        var thumbnail = await workspaceThumbnailService.GetCurrentWorkspaceThumbnailAsync(cancellationToken);
        return thumbnail is null
            ? ApiResults.NotFound(httpContext)
            : ApiResults.File(thumbnail.Data, thumbnail.ContentType);
    }

    /// <summary>Upload or replace the current workspace thumbnail image.</summary>
    /// <response code="204">Thumbnail was saved.</response>
    /// <response code="400">File is missing or empty.</response>
    [Authorize]
    [RequiresWorkspaceParameter("editWorkspace", EditWorkspaceBypass = false)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> PutWorkspaceThumbnail(
        HttpContext httpContext,
        IFormFile? file,
        IWorkspaceThumbnailService workspaceThumbnailService,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_thumbnail", "A non-empty image file is required.");
        }

        await using var stream = file.OpenReadStream();
        await workspaceThumbnailService.SaveCurrentWorkspaceThumbnailAsync(file.ContentType, stream, cancellationToken);
        return ApiResults.NoContent();
    }

    /// <summary>Delete the current workspace thumbnail image.</summary>
    /// <response code="204">Thumbnail was deleted.</response>
    [Authorize]
    [RequiresWorkspaceParameter("editWorkspace", EditWorkspaceBypass = false)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public static async Task<IResult> DeleteWorkspaceThumbnail(
        IWorkspaceThumbnailService workspaceThumbnailService,
        CancellationToken cancellationToken)
    {
        await workspaceThumbnailService.DeleteCurrentWorkspaceThumbnailAsync(cancellationToken);
        return ApiResults.NoContent();
    }

    /// <summary>Update workspace metadata.</summary>
    /// <response code="200">Workspace was updated successfully.</response>
    /// <response code="400">Workspace could not be updated.</response>
    [Authorize]
    [RequiresWorkspaceParameter("editWorkspace", EditWorkspaceBypass = false)]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> UpdateWorkspace(
        HttpContext httpContext,
        UpdateWorkspaceRequest request,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var workspace = request.MapToWorkspace();
        var updated = await workspaceService.UpdateCurrentWorkspaceAsync(workspace, cancellationToken);
        if (!updated)
        {
            return ApiResults.BadRequest(
                httpContext,
                "update_failed",
                "Workspace could not be updated. Ensure you have permission to update this workspace and that the request is valid.");
        }

        var response = workspace.MapToResponse();
        var myMembership = await workspaceService.GetCurrentUserWorkspaceMemberAsync(cancellationToken);
        response.CurrentUserRole = myMembership?.Role.ToString();
        return ApiResults.Ok(response);
    }

    /// <summary>Soft-delete a workspace by archiving it and disconnecting all members from it.</summary>
    /// <response code="204">Workspace was archived successfully.</response>
    /// <response code="404">Workspace was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("editWorkspace", EditWorkspaceBypass = false)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> DeleteWorkspace(
        HttpContext httpContext,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var deleted = await workspaceService.DeleteCurrentWorkspaceAsync(cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    /// <summary>Removes a member from the workspace, or the caller leaves when userId is self.</summary>
    /// <response code="204">Member was removed successfully.</response>
    [Authorize]
    [RequiresWorkspaceParameter("inviteUsers", Notes = "Members may remove themselves without inviteUsers.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> DeleteWorkspaceMember(
        int userId,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        await workspaceService.RemoveCurrentWorkspaceMemberAsync(userId, cancellationToken);
        return ApiResults.NoContent();
    }

    /// <summary>Update a workspace member parameter.</summary>
    /// <response code="200">Workspace member was updated successfully.</response>
    /// <response code="400">Workspace member could not be updated.</response>
    [Authorize]
    [RequiresWorkspaceParameter("inviteUsers")]
    [ProducesResponseType(typeof(WorkspaceMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public static async Task<IResult> UpdateWorkspaceMember(
        HttpContext httpContext,
        int userId,
        UpdateWorkspaceMemberRequest request,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!WorkspacePermissionParameters.TryNormalizeKeys(request.Parameters, out _, out var error))
        {
            return ApiResults.BadRequest(httpContext, "invalid_parameter", error ?? "Invalid parameter keys.");
        }

        var member = request.MapToWorkspaceMember(0, userId);
        var updatedMember = await workspaceService.UpdateCurrentWorkspaceMemberAsync(userId, member, cancellationToken);
        if (updatedMember is null)
        {
            return ApiResults.BadRequest(
                httpContext,
                "update_failed",
                "Workspace member could not be updated. Ensure you have permission to update this member and that the request is valid.");
        }

        return ApiResults.Ok(updatedMember.MapToResponse());
    }
}
