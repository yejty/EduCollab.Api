using EduCollab.Api.Mapping;
using EduCollab.Application.Services.Sessions;
using EduCollab.Contracts.Responses.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class SessionAssetsController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionAssetsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// List assets referenced by a scene within a live session.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.SessionAssets.GetAll)]
        [ProducesResponseType(typeof(SessionAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionAssetsResponse>> GetSessionAssets(
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
                var manifest = await _sessionService.GetSessionAssetsAsync(sessionId, sceneId, cancellationToken);
                return Ok(manifest.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Download an asset ZIP for a live session when includeAssets is enabled.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.SessionAssets.Content)]
        [Produces("application/zip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSessionAssetContent(
            [FromQuery] int sessionId,
            [FromQuery] int sceneId,
            [FromQuery] int assetId,
            CancellationToken cancellationToken)
        {
            if (sessionId <= 0)
                return ApiBadRequest("invalid_session_id", "sessionId is required and must be a positive integer.");
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");
            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            try
            {
                var content = await _sessionService.GetSessionAssetContentAsync(
                    sessionId,
                    sceneId,
                    assetId,
                    cancellationToken);

                var contentType = string.IsNullOrWhiteSpace(content.ContentType)
                    ? "application/zip"
                    : content.ContentType;

                return File(content.Data, contentType, fileDownloadName: "asset.zip");
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
