using EduCollab.Api.Swagger;
using EduCollab.Application.Models;
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
        [RequiresWorkspaceParameter("addFlows", Notes = "Also requires effective access to the flow.")]
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
                var shares = await _flowService.GetFlowGroupSharesAsync(flowId, cancellationToken);
                return Ok(MapShares(shares));
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
        [RequiresWorkspaceParameter("addFlows", Notes = "Also requires manage access to the flow.")]
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

            var added = await _flowService.AddFlowGroupAsync(
                request.FlowId,
                request.GroupId,
                request.IncludeAssets,
                cancellationToken);
            if (!added)
                return ApiNotFound("share_failed", "Flow or group was not found.");

            var shares = await _flowService.GetFlowGroupSharesAsync(request.FlowId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, MapShares(shares));
        }

        /// <summary>
        /// Replace all group shares for a flow.
        /// </summary>
        [Authorize]
        [RequiresWorkspaceParameter("addFlows", Notes = "Also requires manage access to the flow.")]
        [HttpPut(ApiEndpoints.FlowGroups.Update)]
        [ProducesResponseType(typeof(ResourceGroupsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResourceGroupsResponse>> SetFlowGroups(
            [FromQuery] int flowId,
            [FromBody] SetFlowGroupsRequest request,
            CancellationToken cancellationToken)
        {
            if (flowId <= 0)
                return ApiBadRequest("invalid_flow_id", "flowId is required and must be a positive integer.");

            try
            {
                var shares = (request.Groups ?? [])
                    .Select(group => new FlowGroupShare
                    {
                        GroupId = group.GroupId,
                        IncludeAssets = group.IncludeAssets,
                    })
                    .ToList();

                var updated = await _flowService.SetFlowGroupSharesAsync(flowId, shares, cancellationToken);
                if (updated is null)
                    return ApiNotFound("update_failed", "Flow was not found.");

                return Ok(MapShares(updated));
            }
            catch (ArgumentException ex) when (ex.ParamName == "groupIds")
            {
                return ApiBadRequest("invalid_group_id", ex.Message);
            }
        }

        /// <summary>
        /// Remove a flow from a group.
        /// </summary>
        [Authorize]
        [RequiresWorkspaceParameter("addFlows", Notes = "Also requires manage access to the flow.")]
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

        private static ResourceGroupsResponse MapShares(IReadOnlyList<FlowGroupShare> shares) =>
            new()
            {
                GroupIds = shares.Select(share => share.GroupId).ToList(),
                Groups = shares
                    .Select(share => new ResourceGroupShareResponse
                    {
                        GroupId = share.GroupId,
                        IncludeAssets = share.IncludeAssets,
                    })
                    .ToList(),
            };
    }
}
