using Hub.Api.Mapping;
using Hub.Api.Query;
using Hub.Api.Swagger;
using Space.Application.Services.Groups;
using Space.Application.Services.Workspaces;
using Space.Contracts.Requests.Groups;
using Space.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Api.MinimalApis;

internal static class GroupEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.Groups.Create, CreateGroup).AsApi("Groups_Create", "Groups");
        routes.MapGet(ApiEndpoints.Groups.GetAll, GetGroups).AsApi("Groups_GetAll", "Groups");
        routes.MapGet(ApiEndpoints.Groups.GetAllFlat, GetGroupsFlat).AsApi("Groups_GetAllFlat", "Groups");
        routes.MapGet(ApiEndpoints.Groups.Get, GetGroup).AsApi("Groups_Get", "Groups");
        routes.MapPut(ApiEndpoints.Groups.Update, UpdateGroup).AsApi("Groups_Update", "Groups");
        routes.MapDelete(ApiEndpoints.Groups.Delete, DeleteGroup).AsApi("Groups_Delete", "Groups");
        routes.MapGet(ApiEndpoints.Groups.GetAllMembers, GetAllMembers).AsApi("Groups_GetAllMembers", "Groups");
        routes.MapPost(ApiEndpoints.Groups.CreateMember, CreateMember).AsApi("Groups_CreateMember", "Groups");
        routes.MapGet(ApiEndpoints.Groups.GetMember, GetMember).AsApi("Groups_GetMember", "Groups");
        routes.MapDelete(ApiEndpoints.Groups.DeleteMember, DeleteMember).AsApi("Groups_DeleteMember", "Groups");
    }

    /// <summary>Create a new group in the current workspace.</summary>
    /// <response code="201">Group was created.</response>
    /// <response code="400">Group could not be created.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addGroups")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> CreateGroup(
        HttpContext httpContext,
        CreateGroupRequest request,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
        var group = request.MapToGroup();
        var created = await groupService.CreateGroupAsync(group, cancellationToken);
        if (!created)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "Group could not be created.");
        }

        var response = group.MapToResponse();
        response.CurrentUserRole = (await groupService.GetCurrentUserWorkspaceRoleAsync(cancellationToken)).ToString();
        return ApiResults.Created(ApiRoutes.For(ApiEndpoints.Groups.Get, new { groupId = group.Id }), response);
    }

    /// <summary>Browse groups in tree layout at the current level.</summary>
    /// <response code="200">Paged list of groups at the requested tree level.</response>
    [Authorize]
    [RequiresWorkspaceParameter(MembershipOnly = true, Notes = "Returns groups the caller can access via editWorkspace or effective group membership.")]
    [ProducesResponseType(typeof(GroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetGroups(
        HttpContext httpContext,
        [FromQuery] int? parentId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
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

        var groups = await groupService.GetAccessibleGroupsAsync(parentId, cancellationToken);
        var sortedGroups = ResourceSortProfiles.NamedResource.ApplyGroups(groups, sortSpecification);
        return ApiResults.Ok(PaginationApplier.Apply(sortedGroups, paginationSpecification).MapToResponse());
    }

    /// <summary>List all accessible groups in a flat layout.</summary>
    /// <response code="200">Paged flat list of all accessible groups.</response>
    [Authorize]
    [RequiresWorkspaceParameter(MembershipOnly = true, Notes = "Returns groups the caller can access via editWorkspace or effective group membership.")]
    [ProducesResponseType(typeof(GroupsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetGroupsFlat(
        HttpContext httpContext,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
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

        var groups = await groupService.GetAccessibleGroupsFlatAsync(cancellationToken);
        var sortedGroups = ResourceSortProfiles.NamedResource.ApplyGroups(groups, sortSpecification);
        return ApiResults.Ok(PaginationApplier.Apply(sortedGroups, paginationSpecification).MapToResponse());
    }

    /// <summary>Retrieve a group by id.</summary>
    /// <response code="200">Returns the group.</response>
    /// <response code="404">Group was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter(MembershipOnly = true, Notes = "Also requires effective access to the group via editWorkspace or group membership.")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetGroup(
        HttpContext httpContext,
        int groupId,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
        if (EndpointQuery.InvalidGroupId(httpContext, groupId) is { } invalid)
        {
            return invalid;
        }

        var group = await groupService.GetGroupByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var response = group.MapToResponse();
        response.CurrentUserRole = (await groupService.GetCurrentUserWorkspaceRoleAsync(cancellationToken)).ToString();
        return ApiResults.Ok(response);
    }

    /// <summary>Update group metadata.</summary>
    /// <response code="200">Group was updated.</response>
    /// <response code="404">Group was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addGroups", Notes = "Also requires manage access to the group.")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> UpdateGroup(
        HttpContext httpContext,
        int groupId,
        UpdateGroupRequest request,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
        if (EndpointQuery.InvalidGroupId(httpContext, groupId) is { } invalid)
        {
            return invalid;
        }

        var updatedGroup = await groupService.UpdateGroupAsync(request.MapToGroup(groupId), cancellationToken);
        if (updatedGroup is null)
        {
            return ApiResults.NotFound(httpContext, "update_failed", "Group was not found.");
        }

        var response = updatedGroup.MapToResponse();
        response.CurrentUserRole = (await groupService.GetCurrentUserWorkspaceRoleAsync(cancellationToken)).ToString();
        return ApiResults.Ok(response);
    }

    /// <summary>Delete a group.</summary>
    /// <response code="204">Group was deleted.</response>
    /// <response code="404">Group was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addGroups", Notes = "Also requires manage access to the group.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> DeleteGroup(
        HttpContext httpContext,
        int groupId,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
        if (EndpointQuery.InvalidGroupId(httpContext, groupId) is { } invalid)
        {
            return invalid;
        }

        var deleted = await groupService.DeleteGroupAsync(groupId, cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }

    /// <summary>List members of a group.</summary>
    /// <response code="200">Paged list of group members.</response>
    [Authorize]
    [RequiresWorkspaceParameter("seeUsersTab", "inviteUsers", "addGroups", Notes = "Also requires group access rules based on the caller's parameters.")]
    [ProducesResponseType(typeof(GroupMembersResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetAllMembers(
        HttpContext httpContext,
        int groupId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IGroupService groupService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!EndpointQuery.TryParseList(
                httpContext,
                sort,
                page,
                pageSize,
                ResourceSortProfiles.GroupMember.AllowedFields,
                ResourceSortProfiles.GroupMember.Default,
                out var sortSpecification,
                out var paginationSpecification,
                out var problem))
        {
            return problem!;
        }

        var sortedMembers = ResourceSortProfiles.GroupMember.Apply(
            await groupService.GetAllGroupMembersAsync(groupId, cancellationToken),
            sortSpecification);
        var workspaceMembers = await workspaceService.GetCurrentWorkspaceMembersAsync(cancellationToken);
        var workspaceMembersByUserId = workspaceMembers.ToDictionary(member => member.UserId);
        return ApiResults.Ok(PaginationApplier.Apply(sortedMembers, paginationSpecification).MapToResponse(workspaceMembersByUserId));
    }

    /// <summary>Add a workspace member to a group.</summary>
    /// <response code="201">Group member was created.</response>
    /// <response code="400">Member could not be created.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addGroups", Notes = "editWorkspace also allows managing group members. Caller must be a direct member of the group unless they have editWorkspace.")]
    [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> CreateMember(
        HttpContext httpContext,
        int groupId,
        CreateGroupMemberRequest request,
        IGroupService groupService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var created = await groupService.CreateGroupMemberAsync(groupId, request.MapToGroupMember(groupId), cancellationToken);
        if (created is null)
        {
            return ApiResults.BadRequest(httpContext, "creation_failed", "Group member could not be created.");
        }

        var workspaceMember = await workspaceService.GetCurrentWorkspaceMemberAsync(created.UserId, cancellationToken);
        return ApiResults.Created(
            ApiRoutes.For(ApiEndpoints.Groups.GetMember, new { groupId, userId = created.UserId }),
            created.MapToResponse(workspaceMember));
    }

    /// <summary>Retrieve a group member by user id.</summary>
    /// <response code="200">Returns the group member.</response>
    /// <response code="404">Group or member was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("seeUsersTab", "inviteUsers", "addGroups", Notes = "Also requires group access rules based on the caller's parameters.")]
    [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetMember(
        HttpContext httpContext,
        int groupId,
        int userId,
        IGroupService groupService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var member = await groupService.GetGroupMemberAsync(groupId, userId, cancellationToken);
        if (member is null)
        {
            return ApiResults.NotFound(httpContext);
        }

        var workspaceMember = await workspaceService.GetCurrentWorkspaceMemberAsync(userId, cancellationToken);
        return ApiResults.Ok(member.MapToResponse(workspaceMember));
    }

    /// <summary>Remove a member from a group.</summary>
    /// <response code="204">Group member was removed.</response>
    /// <response code="404">Group or member was not found.</response>
    [Authorize]
    [RequiresWorkspaceParameter("addGroups", Notes = "editWorkspace also allows managing group members. Caller must be a direct member of the group unless they have editWorkspace.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> DeleteMember(
        HttpContext httpContext,
        int groupId,
        int userId,
        IGroupService groupService,
        CancellationToken cancellationToken)
    {
        var deleted = await groupService.DeleteGroupMemberAsync(groupId, userId, cancellationToken);
        return deleted ? ApiResults.NoContent() : ApiResults.NotFound(httpContext);
    }
}
