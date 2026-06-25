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
