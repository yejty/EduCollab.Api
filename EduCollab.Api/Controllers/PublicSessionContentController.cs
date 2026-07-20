using EduCollab.Application.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class PublicSessionContentController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public PublicSessionContentController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Download an asset ZIP for a guest session when includeAssets is enabled.
        /// </summary>
        [HttpGet(ApiEndpoints.PublicSessionAssets.Content)]
        [Produces("application/zip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSessionAssetContent(
            [FromQuery] string? guestToken,
            [FromQuery] int sceneId,
            [FromQuery] int assetId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(guestToken))
                return ApiBadRequest("invalid_guest_token", "guestToken is required.");
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");
            if (assetId <= 0)
                return ApiBadRequest("invalid_asset_id", "assetId is required and must be a positive integer.");

            try
            {
                var content = await _sessionService.GetSessionAssetContentForGuestAsync(
                    guestToken,
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

        /// <summary>
        /// Download scene JSON for a guest session.
        /// </summary>
        [HttpGet(ApiEndpoints.PublicSessionScenes.Content)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSessionSceneContent(
            [FromQuery] string? guestToken,
            [FromQuery] int sceneId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(guestToken))
                return ApiBadRequest("invalid_guest_token", "guestToken is required.");
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            try
            {
                var json = await _sessionService.GetSessionSceneContentForGuestAsync(
                    guestToken,
                    sceneId,
                    cancellationToken);
                return Content(json, "application/json");
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
