using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Sessions;
using EduCollab.Contracts.Requests.Sessions;
using EduCollab.Contracts.Responses.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class SessionsController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Create a live collaboration session from a scene or flow.
        /// </summary>
        /// <remarks>
        /// Set <c>allowGuestLink</c> to mint an anonymous share URL returned as <c>guestLinkUrl</c>.
        /// Guests redeem it via <c>POST /api/public/sessions/join</c>.
        /// </remarks>
        [Authorize]
        [HttpPost(ApiEndpoints.Sessions.Create)]
        [ProducesResponseType(typeof(LiveSessionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateSession(
            [FromBody] CreateLiveSessionRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var session = await _sessionService.CreateSessionAsync(
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

                return CreatedAtAction(
                    nameof(GetSession),
                    new { sessionId = session.Id },
                    session.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
            catch (ArgumentException ex)
            {
                return ApiBadRequest("invalid_request", ex.Message);
            }
        }

        /// <summary>
        /// List live sessions shared with the current user (host, user share, or group share).
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.Sessions.GetAll)]
        [ProducesResponseType(typeof(LiveSessionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LiveSessionsResponse>> ListSessions(
            [FromQuery] string? status,
            [FromQuery] string? sharedWith,
            CancellationToken cancellationToken)
        {
            try
            {
                var sessions = await _sessionService.ListSessionsAsync(status, sharedWith, cancellationToken);
                return Ok(sessions.MapToResponse());
            }
            catch (ArgumentException ex)
            {
                return ApiBadRequest("invalid_query", ex.Message);
            }
        }

        /// <summary>
        /// Get a live session the current user can join.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.Sessions.Get)]
        [ProducesResponseType(typeof(LiveSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LiveSessionResponse>> GetSession(
            int sessionId,
            CancellationToken cancellationToken)
        {
            try
            {
                var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
                return Ok(session.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Exchange the caller access token for a short-lived Colyseus join ticket.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.Sessions.JoinTicket)]
        [ProducesResponseType(typeof(SessionJoinTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionJoinTicketResponse>> CreateJoinTicket(
            int sessionId,
            CancellationToken cancellationToken)
        {
            try
            {
                var ticket = await _sessionService.CreateJoinTicketAsync(sessionId, cancellationToken);
                return Ok(ticket.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Content bootstrap package for a session (scenes + asset manifest URLs).
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.Sessions.Bootstrap)]
        [ProducesResponseType(typeof(SessionBootstrapResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionBootstrapResponse>> GetBootstrap(
            int sessionId,
            CancellationToken cancellationToken)
        {
            try
            {
                var bootstrap = await _sessionService.GetBootstrapAsync(sessionId, cancellationToken);
                return Ok(bootstrap.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// End a live session. Host only.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.Sessions.End)]
        [ProducesResponseType(typeof(LiveSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LiveSessionResponse>> EndSession(
            int sessionId,
            CancellationToken cancellationToken)
        {
            try
            {
                var session = await _sessionService.EndSessionAsync(sessionId, cancellationToken);
                return Ok(session.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
