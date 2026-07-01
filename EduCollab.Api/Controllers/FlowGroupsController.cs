using EduCollab.Application.Services.Flows;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Responses.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    public class FlowGroupsController : ApiControllerBase
    {
        private readonly IFlowService _flowService;

        public FlowGroupsController(IFlowService flowService)
        {
            _flowService = flowService;
        }

        /// <summary>
        /// List groups a flow is shared with.
        /// </summary>
        [Authorize]
        [HttpGet(ApiEndpoints.FlowGroups.GetAll)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> GetFlowGroups(
            [FromQuery] int flowId,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            try
            {
                var groupIds = await _flowService.GetFlowGroupIdsAsync(flowId, cancellationToken);
                return Ok(new ResourceGroupsResponse { GroupIds = groupIds });
            }
            catch (KeyNotFoundException)
            {
                return ApiNotFound();
            }
        }

        /// <summary>
        /// Share a flow with a group.
        /// </summary>
        [Authorize]
        [HttpPost(ApiEndpoints.FlowGroups.Create)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ResourceGroupsResponse>> AddFlowGroup(
            [FromBody] AttachFlowGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (request.FlowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId must be a positive integer.");

            if (request.GroupId <= 0)
                return ApiBadRequest("invalid_group_id", "groupId must be a positive integer.");

            var added = await _flowService.AddFlowGroupAsync(request.FlowId, request.GroupId, cancellationToken);
            if (!added)
                return ApiNotFound("share_failed", "Flow or group was not found.");

            var groupIds = await _flowService.GetFlowGroupIdsAsync(request.FlowId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ResourceGroupsResponse { GroupIds = groupIds });
        }

        /// <summary>
        /// Replace all group shares for a flow.
        /// </summary>
        [Authorize]
        [HttpPut(ApiEndpoints.FlowGroups.Update)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> SetFlowGroups(
            [FromQuery] int flowId,
            [FromBody] SetResourceGroupsRequest request,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            var groupIds = await _flowService.SetFlowGroupIdsAsync(flowId, request.GroupIds, cancellationToken);
            if (groupIds is null)
                return ApiNotFound("update_failed", "Flow was not found.");

            return Ok(new ResourceGroupsResponse { GroupIds = groupIds });
        }

        /// <summary>
        /// Remove a flow from a group.
        /// </summary>
        [Authorize]
        [HttpDelete(ApiEndpoints.FlowGroups.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveFlowGroup(
            [FromQuery] int flowId,
            [FromQuery] int groupId,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            if (groupId <= 0)
                return ApiBadRequest("invalid_group_id", "groupId is required and must be a positive integer.");

            var removed = await _flowService.RemoveFlowGroupAsync(flowId, groupId, cancellationToken);
            if (!removed)
                return ApiNotFound();

            return NoContent();
        }
    }
}
