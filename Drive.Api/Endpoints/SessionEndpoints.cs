using Drive.Api.Mapping;
using Space.Application.Services.Sessions;
using Space.Contracts.Requests.Sessions;
using Space.Contracts.Responses.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.MinimalApis;

internal static class SessionEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.Sessions.Create, CreateSession).AsApi("Sessions_Create", "Sessions");
        routes.MapGet(ApiEndpoints.Sessions.GetAll, ListSessions).AsApi("Sessions_GetAll", "Sessions");
        routes.MapGet(ApiEndpoints.Sessions.Get, GetSession).AsApi("Sessions_Get", "Sessions");
        routes.MapPost(ApiEndpoints.Sessions.JoinTicket, CreateJoinTicket).AsApi("Sessions_CreateJoinTicket", "Sessions");
        routes.MapGet(ApiEndpoints.Sessions.Bootstrap, GetBootstrap).AsApi("Sessions_GetBootstrap", "Sessions");
        routes.MapPost(ApiEndpoints.Sessions.End, EndSession).AsApi("Sessions_End", "Sessions");
    }

    /// <summary>Create a live collaboration session from a scene or flow.</summary>
    /// <response code="201">Session was created.</response>
    [Authorize]
    [ProducesResponseType(typeof(LiveSessionResponse), StatusCodes.Status201Created)]
    public static async Task<IResult> CreateSession(
        HttpContext httpContext,
        CreateLiveSessionRequest request,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionService.CreateSessionAsync(
                request.Name,
                request.Description,
                request.SceneId,
                request.FlowId,
                request.GroupIds ?? [],
                request.UserIds ?? [],
                request.IncludeAssets,
                request.AllowGuestLink,
                request.DefaultRole,
                cancellationToken);
            return ApiResults.Created(
                ApiRoutes.For(ApiEndpoints.Sessions.Get, new { sessionId = session.Id }),
                session.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "invalid_request", ex.Message);
        }
    }

    /// <summary>List live sessions shared with the current user.</summary>
    /// <response code="200">Sessions returned.</response>
    [Authorize]
    [ProducesResponseType(typeof(LiveSessionsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> ListSessions(
        HttpContext httpContext,
        [FromQuery] string? status,
        [FromQuery] string? sharedWith,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await sessionService.ListSessionsAsync(status, sharedWith, cancellationToken);
            return ApiResults.Ok(sessions.MapToResponse());
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "invalid_query", ex.Message);
        }
    }

    /// <summary>Get a live session the current user can join.</summary>
    /// <response code="200">Returns the session.</response>
    [Authorize]
    [ProducesResponseType(typeof(LiveSessionResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetSession(
        HttpContext httpContext,
        int sessionId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionService.GetSessionAsync(sessionId, cancellationToken);
            return ApiResults.Ok(session.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Exchange the caller access token for a short-lived Colyseus join ticket.</summary>
    /// <response code="200">Join ticket returned.</response>
    [Authorize]
    [ProducesResponseType(typeof(SessionJoinTicketResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> CreateJoinTicket(
        HttpContext httpContext,
        int sessionId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await sessionService.CreateJoinTicketAsync(sessionId, cancellationToken);
            return ApiResults.Ok(ticket.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Content bootstrap package for a session.</summary>
    /// <response code="200">Bootstrap returned.</response>
    [Authorize]
    [ProducesResponseType(typeof(SessionBootstrapResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetBootstrap(
        HttpContext httpContext,
        int sessionId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var bootstrap = await sessionService.GetBootstrapAsync(sessionId, cancellationToken);
            return ApiResults.Ok(bootstrap.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>End a live session. Host only.</summary>
    /// <response code="200">Session was ended.</response>
    [Authorize]
    [ProducesResponseType(typeof(LiveSessionResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> EndSession(
        HttpContext httpContext,
        int sessionId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionService.EndSessionAsync(sessionId, cancellationToken);
            return ApiResults.Ok(session.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}

internal static class SessionAssetEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.SessionAssets.GetAll, GetSessionAssets).AsApi("SessionAssets_GetAll", "SessionAssets");
        routes.MapGet(ApiEndpoints.SessionAssets.Content, GetSessionAssetContent).AsApi("SessionAssets_GetContent", "SessionAssets");
    }

    /// <summary>List assets referenced by a scene within a live session.</summary>
    /// <response code="200">Session asset manifest.</response>
    [Authorize]
    [ProducesResponseType(typeof(SessionAssetsResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetSessionAssets(
        HttpContext httpContext,
        [FromQuery] int sessionId,
        [FromQuery] int sceneId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_session_id", "sessionId is required and must be a positive integer.");
        }

        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        try
        {
            var manifest = await sessionService.GetSessionAssetsAsync(sessionId, sceneId, cancellationToken);
            return ApiResults.Ok(manifest.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Download an asset ZIP for a live session when includeAssets is enabled.</summary>
    /// <response code="200">Asset ZIP bytes.</response>
    [Authorize]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetSessionAssetContent(
        HttpContext httpContext,
        [FromQuery] int sessionId,
        [FromQuery] int sceneId,
        [FromQuery] int assetId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_session_id", "sessionId is required and must be a positive integer.");
        }

        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        if (assetId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_asset_id", "assetId is required and must be a positive integer.");
        }

        try
        {
            var content = await sessionService.GetSessionAssetContentAsync(sessionId, sceneId, assetId, cancellationToken);
            var contentType = string.IsNullOrWhiteSpace(content.ContentType) ? "application/zip" : content.ContentType;
            return ApiResults.File(content.Data, contentType, "asset.zip");
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}

internal static class SessionSceneEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.SessionScenes.Content, GetSessionSceneContent).AsApi("SessionScenes_GetContent", "SessionScenes");
    }

    /// <summary>Download scene JSON content in a live session context.</summary>
    /// <response code="200">Scene JSON.</response>
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetSessionSceneContent(
        HttpContext httpContext,
        [FromQuery] int sessionId,
        [FromQuery] int sceneId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_session_id", "sessionId is required and must be a positive integer.");
        }

        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        try
        {
            var json = await sessionService.GetSessionSceneContentAsync(sessionId, sceneId, cancellationToken);
            return ApiResults.Text(json, "application/json");
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}

internal static class PublicSessionEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.PublicSessions.Join, Join)
            .AsApi("PublicSessions_Join", "PublicSessions")
            .AllowAnonymous();
        routes.MapGet(ApiEndpoints.PublicSessions.Bootstrap, GetBootstrap)
            .AsApi("PublicSessions_GetBootstrap", "PublicSessions")
            .AllowAnonymous();
    }

    /// <summary>Join a live session anonymously via guest invite link.</summary>
    /// <response code="200">Join ticket returned.</response>
    [AllowAnonymous]
    [ProducesResponseType(typeof(SessionJoinTicketResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> Join(
        HttpContext httpContext,
        PublicSessionJoinRequest request,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await sessionService.JoinAsGuestAsync(request.GuestToken, request.DisplayName, cancellationToken);
            return ApiResults.Ok(ticket.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
        catch (ArgumentException ex)
        {
            return ApiResults.BadRequest(httpContext, "invalid_request", ex.Message);
        }
    }

    /// <summary>Content bootstrap for a guest invite link.</summary>
    /// <response code="200">Bootstrap returned.</response>
    [AllowAnonymous]
    [ProducesResponseType(typeof(SessionBootstrapResponse), StatusCodes.Status200OK)]
    public static async Task<IResult> GetBootstrap(
        HttpContext httpContext,
        [FromQuery] string? guestToken,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return ApiResults.BadRequest(httpContext, "invalid_guest_token", "guestToken is required.");
        }

        try
        {
            var bootstrap = await sessionService.GetBootstrapForGuestAsync(guestToken, cancellationToken);
            return ApiResults.Ok(bootstrap.MapToResponse());
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}

internal static class PublicSessionContentEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.PublicSessionAssets.Content, GetSessionAssetContent)
            .AsApi("PublicSessionContent_GetAsset", "PublicSessionContent")
            .AllowAnonymous();
        routes.MapGet(ApiEndpoints.PublicSessionScenes.Content, GetSessionSceneContent)
            .AsApi("PublicSessionContent_GetScene", "PublicSessionContent")
            .AllowAnonymous();
    }

    /// <summary>Download an asset ZIP for a guest session when includeAssets is enabled.</summary>
    /// <response code="200">Asset ZIP bytes.</response>
    [AllowAnonymous]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetSessionAssetContent(
        HttpContext httpContext,
        [FromQuery] string? guestToken,
        [FromQuery] int sceneId,
        [FromQuery] int assetId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return ApiResults.BadRequest(httpContext, "invalid_guest_token", "guestToken is required.");
        }

        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        if (assetId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_asset_id", "assetId is required and must be a positive integer.");
        }

        try
        {
            var content = await sessionService.GetSessionAssetContentForGuestAsync(guestToken, sceneId, assetId, cancellationToken);
            var contentType = string.IsNullOrWhiteSpace(content.ContentType) ? "application/zip" : content.ContentType;
            return ApiResults.File(content.Data, contentType, "asset.zip");
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Download scene JSON for a guest session.</summary>
    /// <response code="200">Scene JSON.</response>
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> GetSessionSceneContent(
        HttpContext httpContext,
        [FromQuery] string? guestToken,
        [FromQuery] int sceneId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return ApiResults.BadRequest(httpContext, "invalid_guest_token", "guestToken is required.");
        }

        if (sceneId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_scene_id", "sceneId is required and must be a positive integer.");
        }

        try
        {
            var json = await sessionService.GetSessionSceneContentForGuestAsync(guestToken, sceneId, cancellationToken);
            return ApiResults.Text(json, "application/json");
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}

internal static class InternalSessionEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(ApiEndpoints.InternalSessions.RoomStarted, RoomStarted)
            .AsApi("InternalSessions_RoomStarted", "InternalSessions")
            .AllowAnonymous()
            .AddEndpointFilter<InternalApiKeyEndpointFilter>();
        routes.MapPost(ApiEndpoints.InternalSessions.RoomEnded, RoomEnded)
            .AsApi("InternalSessions_RoomEnded", "InternalSessions")
            .AllowAnonymous()
            .AddEndpointFilter<InternalApiKeyEndpointFilter>();
    }

    /// <summary>Colyseus webhook: room created / first client joined. Sets session status to Active.</summary>
    /// <response code="204">Session was marked active.</response>
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> RoomStarted(
        HttpContext httpContext,
        int sessionId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_session_id", "sessionId must be a positive integer.");
        }

        try
        {
            await sessionService.MarkRoomStartedAsync(sessionId, cancellationToken);
            return ApiResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }

    /// <summary>Colyseus webhook: room disposed. Sets session status to Ended.</summary>
    /// <response code="204">Session was marked ended.</response>
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<IResult> RoomEnded(
        HttpContext httpContext,
        int sessionId,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            return ApiResults.BadRequest(httpContext, "invalid_session_id", "sessionId must be a positive integer.");
        }

        try
        {
            await sessionService.MarkRoomEndedAsync(sessionId, cancellationToken);
            return ApiResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return ApiResults.NotFound(httpContext);
        }
    }
}
