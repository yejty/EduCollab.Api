using EduCollab.Api.Mapping;
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
        /// List scenes linked to a flow in sort order.
        /// </summary>
        /// <param name="flowId">Flow identifier (required).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Scenes linked to the flow.</response>
        /// <response code="400">Invalid flow id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this flow.</response>
        /// <response code="404">Flow was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.FlowScenes.GetAll)]
        [ProducesResponseType(typeof(FlowScenesResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FlowScenesResponse>> GetFlowScenes([FromQuery] int flowId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId must be a positive integer.");

            var flowScenes = await _flowService.GetFlowScenesAsync(flowId, cancellationToken);
            return Ok(flowScenes.MapToResponse());
        }

        /// <summary>
        /// Link a scene to a flow.
        /// </summary>
        /// <param name="request">Flow and scene identifiers plus optional sort order.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Scene was linked to the flow.</response>
        /// <response code="400">Link could not be created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot modify this flow.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.FlowScenes.Create)]
        [ProducesResponseType(typeof(FlowSceneResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddFlowScene([FromBody] CreateFlowSceneRequest request, CancellationToken cancellationToken)
        {
            var created = await _flowService.AddFlowSceneAsync(request.FlowId, request.SceneId, request.SortOrder, cancellationToken);
            if (created is null)
                return ApiBadRequest("creation_failed", "Flow scene link could not be created.");

            return StatusCode(StatusCodes.Status201Created, created.MapToResponse());
        }

        /// <summary>
        /// Unlink a scene from a flow.
        /// </summary>
        /// <param name="flowId">Flow identifier (required).</param>
        /// <param name="sceneId">Scene identifier (required).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Scene was unlinked from the flow.</response>
        /// <response code="400">Invalid flow or scene id.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot modify this flow.</response>
        /// <response code="404">Flow-scene link was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.FlowScenes.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveFlowScene([FromQuery] int flowId, [FromQuery] int sceneId, CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId must be a positive integer.");
            if (sceneId <= 0)
                return ApiBadRequest("invalid_scene_id", "sceneId must be a positive integer.");

            var removed = await _flowService.RemoveFlowSceneAsync(flowId, sceneId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
