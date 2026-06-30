using EduCollab.Api.Mapping;
using EduCollab.Api.Query;
using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Requests.Flows;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Flows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class FlowsController : ApiControllerBase
    {
        private readonly IFlowService _flowService;

        public FlowsController(IFlowService flowService)
        {
            _flowService = flowService;
        }

        /// <summary>
        /// Create a new flow in the current workspace.
        /// </summary>
        /// <param name="request">Flow creation payload including group placement.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Flow was created.</response>
        /// <response code="400">Flow could not be created.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot create flows in this workspace.</response>
        [Authorize]
        [HttpPost(ApiEndpoints.Flows.Create)]
        [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateFlow([FromBody] CreateFlowRequest request, CancellationToken cancellationToken)
        {
            var flow = request.MapToFlow();
            var created = await _flowService.CreateFlowAsync(flow, cancellationToken);
            if (!created)
                return ApiBadRequest("creation_failed", "Flow could not be created.");

            var response = flow.MapToResponse();
            response.CanManage = await _flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);
            return CreatedAtAction(nameof(GetFlow), new { flowId = flow.Id }, response);
        }

        /// <summary>
        /// List flows in the current workspace.
        /// </summary>
        /// <param name="owner">Optional filter. Set to <c>me</c> to return only flows owned by the caller.</param>
        /// <param name="sort">Optional sort field (<c>name</c>, <c>createdAt</c>, <c>updatedAt</c>, <c>id</c>). Prefix with <c>-</c> for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Paged list of flows.</response>
        /// <response code="400">Invalid filter, sort, or pagination.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access flows in this workspace.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Flows.GetAll)]
        [ProducesResponseType(typeof(FlowsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FlowsResponse>> GetFlows(
            [FromQuery] string? owner,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            if (!OwnerQueryParser.TryParse(owner, out var ownerIsCurrentUser, out var ownerError))
                return ApiBadRequest("invalid_filter", ownerError!);

            if (!TryParseListQuery(
                    sort,
                    page,
                    pageSize,
                    ResourceSortProfiles.NamedResource.AllowedFields,
                    ResourceSortProfiles.NamedResource.Default,
                    out var sortSpecification,
                    out var paginationSpecification,
                    out var problem))
            {
                return problem!;
            }

            var flows = ownerIsCurrentUser
                ? await _flowService.GetMyFlowsAsync(cancellationToken)
                : await _flowService.GetAllFlowsAsync(cancellationToken);

            var sorted = ResourceSortProfiles.NamedResource.ApplyFlows(flows, sortSpecification);
            var paged = PaginationApplier.Apply(sorted, paginationSpecification);
            var response = paged.MapToResponse();

            foreach (var flow in response.Flows)
                flow.CanManage = await _flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Retrieve a flow by id.
        /// </summary>
        /// <param name="flowId">Flow identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the flow.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this flow.</response>
        /// <response code="404">Flow was not found.</response>
        [Authorize]
        [HttpGet(ApiEndpoints.Flows.Get)]
        [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FlowResponse>> GetFlow(int flowId, CancellationToken cancellationToken)
        {
            var flow = await _flowService.GetFlowByIdAsync(flowId, cancellationToken);
            if (flow is null)
                return ApiNotFound();

            var response = flow.MapToResponse();
            response.CanManage = await _flowService.CanCurrentUserManageFlowAsync(flow.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Update flow metadata and group placement.
        /// </summary>
        /// <param name="flowId">Flow identifier.</param>
        /// <param name="request">Flow update payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Flow was updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this flow.</response>
        /// <response code="404">Flow was not found.</response>
        [Authorize]
        [HttpPut(ApiEndpoints.Flows.Update)]
        [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FlowResponse>> UpdateFlow(int flowId, [FromBody] UpdateFlowRequest request, CancellationToken cancellationToken)
        {
            var flow = request.MapToFlow(flowId);
            var updated = await _flowService.UpdateFlowAsync(flow, cancellationToken);
            if (updated is null)
                return ApiNotFound("update_failed", "Flow was not found.");

            var response = updated.MapToResponse();
            response.CanManage = await _flowService.CanCurrentUserManageFlowAsync(updated.OwnerUserId, cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Delete a flow.
        /// </summary>
        /// <param name="flowId">Flow identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Flow was deleted.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot delete this flow.</response>
        /// <response code="404">Flow was not found.</response>
        [Authorize]
        [HttpDelete(ApiEndpoints.Flows.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteFlow(int flowId, CancellationToken cancellationToken)
        {
            var deleted = await _flowService.DeleteFlowAsync(flowId, cancellationToken);
            if (!deleted)
                return ApiNotFound();

            return NoContent();
        }
    }
}
