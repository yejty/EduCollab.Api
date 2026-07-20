using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Sessions;
using EduCollab.Contracts.Requests.Sessions;
using EduCollab.Contracts.Responses.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class PublicSessionsController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public PublicSessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Join a live session anonymously via guest invite link.
        /// </summary>
        /// <remarks>
        /// Returns a short-lived Colyseus join ticket. Guests connect with
        /// <c>joinOrCreate('collab', { sessionId, joinTicket })</c>.
        /// </remarks>
        [HttpPost(ApiEndpoints.PublicSessions.Join)]
        [ProducesResponseType(typeof(SessionJoinTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionJoinTicketResponse>> Join(
            [FromBody] PublicSessionJoinRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var ticket = await _sessionService.JoinAsGuestAsync(
                    request.GuestToken,
                    request.DisplayName,
                    cancellationToken);
                return Ok(ticket.MapToResponse());
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
        /// Content bootstrap for a guest invite link (scenes + asset URLs).
        /// </summary>
        [HttpGet(ApiEndpoints.PublicSessions.Bootstrap)]
        [ProducesResponseType(typeof(SessionBootstrapResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionBootstrapResponse>> GetBootstrap(
            [FromQuery] string? guestToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(guestToken))
                return ApiBadRequest("invalid_guest_token", "guestToken is required.");

            try
            {
                var bootstrap = await _sessionService.GetBootstrapForGuestAsync(guestToken, cancellationToken);
                return Ok(bootstrap.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
