using EduCollab.Api.Security;
using EduCollab.Application.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [RequireInternalApiKey]
    public class InternalSessionsController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public InternalSessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Colyseus webhook: room created / first client joined. Sets session status to Active.
        /// </summary>
        [HttpPost(ApiEndpoints.InternalSessions.RoomStarted)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RoomStarted(int sessionId, CancellationToken cancellationToken)
        {
            if (sessionId <= 0)
                return ApiBadRequest("invalid_session_id", "sessionId must be a positive integer.");

            try
            {
                await _sessionService.MarkRoomStartedAsync(sessionId, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Colyseus webhook: room disposed. Sets session status to Ended.
        /// </summary>
        [HttpPost(ApiEndpoints.InternalSessions.RoomEnded)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RoomEnded(int sessionId, CancellationToken cancellationToken)
        {
            if (sessionId <= 0)
                return ApiBadRequest("invalid_session_id", "sessionId must be a positive integer.");

            try
            {
                await _sessionService.MarkRoomEndedAsync(sessionId, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
