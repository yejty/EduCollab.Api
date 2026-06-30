using EduCollab.Api.Mapping;

using EduCollab.Api.Query;

using EduCollab.Api.Requests.Scenes;

using EduCollab.Application.Services.Scenes;

using EduCollab.Contracts.Requests.Scenes;

using EduCollab.Contracts.Responses;

using EduCollab.Contracts.Responses.Scenes;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace EduCollab.Api.Controllers

{

    [ApiController]

    public class ScenesController : ApiControllerBase

    {

        private readonly ISceneService _sceneService;



        public ScenesController(ISceneService sceneService)

        {

            _sceneService = sceneService;

        }



        private async Task PopulateAccessMetadataAsync(SceneResponse scene, CancellationToken cancellationToken)

        {

            scene.CanEdit = await _sceneService.CanCurrentUserManageSceneAsync(scene.OwnerUserId, cancellationToken);

            scene.CanManage = scene.CanEdit;

        }



        /// <summary>
        /// Create a new scene in the specified group.
        /// </summary>
        /// <remarks>
        /// Send <c>jsonContent</c> inline in the JSON body. Scene objects reference workspace assets via an <c>assetId</c>
        /// property anywhere in the JSON tree. Use <see cref="CreateSceneFromForm"/> to upload a <c>.json</c> file instead.
        /// </remarks>
        /// <param name="request">Scene creation payload including target <c>groupId</c> and inline <c>jsonContent</c>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Scene was created.</response>
        /// <response code="400">Scene could not be created or references an invalid asset.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot create scenes in this group.</response>
        [Authorize]

        [HttpPost(ApiEndpoints.Scenes.Create)]

        [Consumes("application/json")]

        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]

        public async Task<IActionResult> CreateScene([FromBody] CreateSceneRequest request, CancellationToken cancellationToken)

        {

            var scene = request.MapToScene();

            var created = await _sceneService.CreateSceneAsync(scene, request.GroupId, cancellationToken);

            if (!created)

                return ApiBadRequest("creation_failed", "Scene could not be created.");



            var response = scene.MapToResponse();

            await PopulateAccessMetadataAsync(response, cancellationToken);

            return CreatedAtAction(nameof(GetScene), new { sceneId = scene.Id }, response);

        }



        /// <summary>
        /// Create a new scene by uploading scene JSON as a file.
        /// </summary>
        /// <remarks>
        /// Alternative to <see cref="CreateScene"/> for desktop exporters. Provide either <c>jsonFile</c> (multipart file)
        /// or a <c>jsonContent</c> form field. Scene objects reference workspace assets via an <c>assetId</c> property.
        /// </remarks>
        /// <param name="request">Multipart form with scene metadata and JSON content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Scene was created.</response>
        /// <response code="400">Metadata, JSON content, or asset references are invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot create scenes in this group.</response>
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost(ApiEndpoints.Scenes.Create)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateSceneFromForm([FromForm] CreateSceneFormRequest request, CancellationToken cancellationToken)

        {

            if (request.GroupId <= 0)

                return ApiBadRequest("invalid_group", "GroupId is required.");



            if (string.IsNullOrWhiteSpace(request.Name))

                return ApiBadRequest("invalid_name", "Name is required.");



            string jsonContent;

            try

            {

                jsonContent = SceneFormContentResolver.ResolveJsonContent(request.JsonContent, request.JsonFile);

                SceneFormContentResolver.ParseJsonContent(jsonContent);

            }

            catch (ArgumentException ex)

            {

                return ApiBadRequest("invalid_json_content", ex.Message);

            }



            var createRequest = new CreateSceneRequest

            {

                Name = request.Name.Trim(),

                Description = request.Description,

                GroupId = request.GroupId,

                JsonContent = SceneFormContentResolver.ParseJsonContent(jsonContent),

            };



            return await CreateScene(createRequest, cancellationToken);

        }



        /// <summary>
        /// List accessible scenes in the current workspace.
        /// </summary>
        /// <param name="owner">Optional filter. Set to <c>me</c> to return only scenes owned by the caller.</param>
        /// <param name="sort">Optional sort field (<c>name</c>, <c>createdAt</c>, <c>updatedAt</c>, <c>id</c>). Prefix with <c>-</c> for descending.</param>
        /// <param name="page">1-based page index. Default: 1.</param>
        /// <param name="pageSize">Page size. Default: 20, maximum: 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Paged list of scenes.</response>
        /// <response code="400">Invalid filter, sort, or pagination.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access scenes in this workspace.</response>
        [Authorize]

        [HttpGet(ApiEndpoints.Scenes.GetAll)]

        [ProducesResponseType(typeof(ScenesResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<ScenesResponse>> GetScenes(

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



            var scenes = ownerIsCurrentUser

                ? await _sceneService.GetMyScenesAsync(cancellationToken)

                : await _sceneService.GetAllScenesAsync(cancellationToken);



            var sorted = ResourceSortProfiles.NamedResource.ApplyScenes(scenes, sortSpecification);

            var paged = PaginationApplier.Apply(sorted, paginationSpecification);

            var response = paged.MapToResponse();



            foreach (var scene in response.Scenes)

                await PopulateAccessMetadataAsync(scene, cancellationToken);



            return Ok(response);

        }



        /// <summary>
        /// Retrieve a scene by id.
        /// </summary>
        /// <param name="sceneId">Scene identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the scene.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot access this scene.</response>
        /// <response code="404">Scene was not found.</response>
        [Authorize]

        [HttpGet(ApiEndpoints.Scenes.Get)]

        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<SceneResponse>> GetScene(int sceneId, CancellationToken cancellationToken)

        {

            var scene = await _sceneService.GetSceneByIdAsync(sceneId, cancellationToken);

            if (scene is null)

                return ApiNotFound();



            var response = scene.MapToResponse();

            await PopulateAccessMetadataAsync(response, cancellationToken);

            return Ok(response);

        }



        /// <summary>
        /// Update scene metadata, group placement, and inline JSON content.
        /// </summary>
        /// <param name="sceneId">Scene identifier.</param>
        /// <param name="request">Scene update payload including <c>jsonContent</c>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Scene was updated.</response>
        /// <response code="400">Scene update payload or asset references are invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this scene.</response>
        /// <response code="404">Scene was not found.</response>
        [Authorize]

        [HttpPut(ApiEndpoints.Scenes.Update)]

        [Consumes("application/json")]

        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]

        public async Task<ActionResult<SceneResponse>> UpdateScene(int sceneId, [FromBody] UpdateSceneRequest request, CancellationToken cancellationToken)

        {

            var scene = request.MapToScene(sceneId);

            var updated = await _sceneService.UpdateSceneAsync(scene, cancellationToken);

            if (updated is null)

                return ApiNotFound("update_failed", "Scene was not found.");



            var response = updated.MapToResponse();

            await PopulateAccessMetadataAsync(response, cancellationToken);

            return Ok(response);

        }



        /// <summary>
        /// Update a scene by uploading scene JSON as a file.
        /// </summary>
        /// <param name="sceneId">Scene identifier.</param>
        /// <param name="request">Multipart form with scene metadata and JSON content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Scene was updated.</response>
        /// <response code="400">Metadata, JSON content, or asset references are invalid.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot update this scene.</response>
        /// <response code="404">Scene was not found.</response>
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPut(ApiEndpoints.Scenes.Update)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SceneResponse>> UpdateSceneFromForm(int sceneId, [FromForm] UpdateSceneFormRequest request, CancellationToken cancellationToken)

        {

            if (string.IsNullOrWhiteSpace(request.Name))

                return ApiBadRequest("invalid_name", "Name is required.");



            if (request.GroupId <= 0)

                return ApiBadRequest("invalid_group", "GroupId is required.");



            string jsonContent;

            try

            {

                jsonContent = SceneFormContentResolver.ResolveJsonContent(request.JsonContent, request.JsonFile);

                SceneFormContentResolver.ParseJsonContent(jsonContent);

            }

            catch (ArgumentException ex)

            {

                return ApiBadRequest("invalid_json_content", ex.Message);

            }



            var updateRequest = new UpdateSceneRequest

            {

                Name = request.Name.Trim(),

                Description = request.Description,

                GroupId = request.GroupId,

                JsonContent = SceneFormContentResolver.ParseJsonContent(jsonContent),

            };



            return await UpdateScene(sceneId, updateRequest, cancellationToken);

        }



        /// <summary>
        /// Delete a scene.
        /// </summary>
        /// <param name="sceneId">Scene identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Scene was deleted.</response>
        /// <response code="401">Caller is not authenticated.</response>
        /// <response code="403">Caller cannot delete this scene.</response>
        /// <response code="404">Scene was not found.</response>
        [Authorize]

        [HttpDelete(ApiEndpoints.Scenes.Delete)]

        [ProducesResponseType(StatusCodes.Status204NoContent)]

        public async Task<IActionResult> DeleteScene(int sceneId, CancellationToken cancellationToken)

        {

            var deleted = await _sceneService.DeleteSceneAsync(sceneId, cancellationToken);

            if (!deleted)

                return ApiNotFound();



            return NoContent();

        }

    }

}


