using EduCollab.Application.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class SessionScenesController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionScenesController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Download scene JSON content in a live session context.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.SessionScenes.Content)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSessionSceneContent(
            [FromQuery] int sessionId,
            [FromQuery] int sceneId,
            CancellationToken cancellationToken)
        {
            if (sessionId <= 0)
                return ApiBadRequest("invalid_session_id", "sessionId is required and must be a positive integer.");
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            try
            {
                var json = await _sessionService.GetSessionSceneContentAsync(sessionId, sceneId, cancellationToken);
                return Content(json, "application/json");
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
