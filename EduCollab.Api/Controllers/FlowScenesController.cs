using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Responses.Flows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class FlowScenesController : ApiControllerBase
    {
        private readonly IFlowService _flowService;

        public FlowScenesController(IFlowService flowService)
        {
            _flowService = flowService;
        }

        /// <summary>
        /// List scenes attached to a flow.
        /// </summary>
        /// <remarks>
        /// Peer-visible scenes have <c>canViewDirectly=true</c> and should be loaded via
        /// <c>GET /scenes/{sceneId}</c>. Contextual scenes include a short-lived
        /// <c>downloadToken</c> when the flow share has <c>includeAssets=true</c>.
        /// </remarks>
        [Authorize]
        [HttpGet(ApiEndpoints.FlowScenes.GetAll)]
        [ProducesResponseType(typeof(FlowScenesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FlowScenesResponse>> GetFlowScenes(
            [FromQuery] int flowId,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            if (!TryParseListQuery(
                    sort,
                    page,
                    pageSize,
                    ResourceSortProfiles.FlowScene.AllowedFields,
                    ResourceSortProfiles.FlowScene.Default,
                    out var sortSpecification,
                    out var paginationSpecification,
                    out var problem))
            {
                return problem!;
            }

            try
            {
                var manifest = await _flowService.GetFlowScenesAsync(flowId, cancellationToken);
                var sorted = ResourceSortProfiles.FlowScene.Apply(manifest.Scenes, sortSpecification);
                var paged = PaginationApplier.Apply(sorted, paginationSpecification);
                return Ok(paged.MapToResponse(manifest));
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Download scene JSON using a short-lived flow-scenes download token.
        /// </summary>
        /// <remarks>
        /// Requires the caller access token and a <c>downloadToken</c> from the flow-scenes manifest.
        /// Peer-visible scenes should use <c>GET /scenes/{sceneId}</c> instead.
        /// </remarks>
        [Authorize]
        [HttpGet(ApiEndpoints.FlowScenes.Content)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFlowSceneContent(
            [FromQuery] string? token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiBadRequest("invalid_download_token", "token is required.");

            try
            {
                var content = await _flowService.GetFlowSceneContentAsync(token, cancellationToken);
                if (content is null)
                    return ApiNotFound();

                var contentType = string.IsNullOrWhiteSpace(content.ContentType)
                    ? "application/json"
                    : content.ContentType;
                var fileName = string.IsNullOrWhiteSpace(content.FileName)
                    ? "scene.json"
                    : content.FileName;

                return File(content.Data, contentType, fileDownloadName: fileName);
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
