using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Requests.Flows;
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
        /// Authoritative manifest for running a flow. Each entry includes <c>usableInFlow</c> and
        /// <c>canViewDirectly</c> flags for scene access in flow context. Use <see cref="GetFlowSceneContent"/>
        /// to load scene JSON when <c>canViewDirectly</c> is false.
        /// </remarks>
        /// <param name="flowId">Flow identifier (required).</param>
        /// <param name="sort">Optional sort field (<c>name</c>, <c>sceneId</c>). Prefix with <c>-</c> for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Paged list of flow scenes.</response>
        /// <response code="400">Invalid flow id, sort, or pagination.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this flow.</response>
        /// <response code="404">Flow was not found.</response>
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
                var scenes = await _flowService.GetFlowScenesAsync(flowId, cancellationToken);
                var sorted = ResourceSortProfiles.FlowScene.Apply(scenes, sortSpecification);
                var paged = PaginationApplier.Apply(sorted, paginationSpecification);
                return Ok(paged.MapToResponse());
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Attach a scene to a flow.
        /// </summary>
        /// <param name="request">Flow and scene identifiers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Scene was attached to the flow.</response>
        /// <response code="400">Invalid flow or scene id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot modify this flow.</response>
        /// <response code="404">Flow or scene was not found.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.FlowScenes.Create)]
        [ProducesResponseType(typeof(FlowSceneResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FlowSceneResponse>> AttachFlowScene(
            [FromBody] AttachFlowSceneRequest request,
            CancellationToken cancellationToken)
        {
            if (request.FlowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId must be a positive integer.");

            if (request.SceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId must be a positive integer.");

            var attached = await _flowService.AttachFlowSceneAsync(request.FlowId, request.SceneId, cancellationToken);
            if (attached is null)
                return ApiNotFound("attach_failed", "Flow or scene was not found.");

            return StatusCode(StatusCodes.Status201Created, attached.MapToResponse());
        }

        /// <summary>
        /// Detach a scene from a flow.
        /// </summary>
        /// <param name="flowId">Flow identifier (required).</param>
        /// <param name="sceneId">Scene identifier (required).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Scene was detached from the flow.</response>
        /// <response code="400">Invalid flow or scene id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot modify this flow.</response>
        /// <response code="404">Flow-scene link was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.FlowScenes.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DetachFlowScene(
            [FromQuery] int flowId,
            [FromQuery] int sceneId,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            var detached = await _flowService.DetachFlowSceneAsync(flowId, sceneId, cancellationToken);
            if (!detached)
                return ApiNotFound();

            return NoContent();
        }

        /// <summary>
        /// Load scene JSON in flow context.
        /// </summary>
        /// <remarks>
        /// Use when the manifest lists a scene with <c>usableInFlow: true</c> and <c>canViewDirectly: false</c>.
        /// Standalone <c>GET /scenes/{sceneId}</c> remains direct-access only.
        /// </remarks>
        /// <param name="flowId">Flow identifier (required).</param>
        /// <param name="sceneId">Scene identifier (required).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Scene JSON content.</response>
        /// <response code="400">Invalid flow or scene id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this flow.</response>
        /// <response code="404">Flow, scene, or content was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.FlowScenes.Content)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFlowSceneContent(
            [FromQuery] int flowId,
            [FromQuery] int sceneId,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId is required and must be a positive integer.");

            try
            {
                var content = await _flowService.GetFlowSceneContentAsync(flowId, sceneId, cancellationToken);
                if (content is null)
                    return ApiNotFound();

                return Content(content, "application/json");
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }
    }
}
