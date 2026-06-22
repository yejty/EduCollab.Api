using EduCollab.Api.Problems;
using EduCollab.Api.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EduCollab.Api.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected bool TryParsePagination(
            int? page,
            int? pageSize,
            out PaginationSpecification specification,
            out ObjectResult? problem)
        {
            if (PaginationQueryParser.TryParse(page, pageSize, out specification, out var errorDetail))
            {
                problem = null;
                return true;
            }

            problem = ApiBadRequest("invalid_pagination", errorDetail);
            return false;
        }

        protected bool TryParseListQuery(
            string? sort,
            int? page,
            int? pageSize,
            IReadOnlyCollection<string> allowedSortFields,
            SortSpecification defaultSort,
            out SortSpecification sortSpecification,
            out PaginationSpecification paginationSpecification,
            out ObjectResult? problem)
        {
            if (!TryParseSort(sort, allowedSortFields, defaultSort, out sortSpecification, out problem))
            {
                paginationSpecification = null!;
                return false;
            }

            if (!TryParsePagination(page, pageSize, out paginationSpecification, out problem))
            {
                return false;
            }

            problem = null;
            return true;
        }

        protected bool TryParseSort(
            string? sort,
            IReadOnlyCollection<string> allowedFields,
            SortSpecification defaultSort,
            out SortSpecification specification,
            out ObjectResult? problem)
        {
            if (SortQueryParser.TryParse(sort, allowedFields, defaultSort, out specification, out var errorDetail))
            {
                problem = null;
                return true;
            }

            problem = ApiBadRequest("invalid_sort", errorDetail);
            return false;
        }

        protected ObjectResult ApiProblem(int statusCode, string error, string detail)
        {
            var problem = ApiProblemDetailsFactory.Create(HttpContext, statusCode, error, detail);
            return CreateProblemResult(statusCode, problem);
        }

        protected ObjectResult ApiBadRequest(string error, string detail) =>
            ApiProblem(StatusCodes.Status400BadRequest, error, detail);

        protected ObjectResult ApiUnauthorized(string error, string detail) =>
            ApiProblem(StatusCodes.Status401Unauthorized, error, detail);

        protected ObjectResult ApiForbidden(string error, string detail) =>
            ApiProblem(StatusCodes.Status403Forbidden, error, detail);

        protected ObjectResult ApiNotFound(
            string error = "not_found",
            string detail = "The requested resource was not found.") =>
            ApiProblem(StatusCodes.Status404NotFound, error, detail);

        protected ObjectResult ApiPreconditionFailed(string error, string detail) =>
            ApiProblem(StatusCodes.Status412PreconditionFailed, error, detail);

        protected bool TryGetRequiredIfMatchHeader(out string ifMatch, out ObjectResult? problem)
        {
            if (!Request.Headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.IfMatch, out var values)
                || string.IsNullOrWhiteSpace(values.ToString()))
            {
                ifMatch = string.Empty;
                problem = ApiBadRequest(
                    "precondition_required",
                    "If-Match header is required for this operation.");
                return false;
            }

            ifMatch = Http.EntityTagHeaderParser.Normalize(values.ToString());
            problem = null;
            return true;
        }

        public override ActionResult ValidationProblem(ModelStateDictionary modelStateDictionary)
        {
            var problem = ApiProblemDetailsFactory.CreateValidationProblem(HttpContext, modelStateDictionary);
            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { ApiProblemDetailsFactory.ProblemJsonMediaType },
            };
        }

        private static ObjectResult CreateProblemResult(int statusCode, ProblemDetails problem) =>
            new(problem)
            {
                StatusCode = statusCode,
                ContentTypes = { ApiProblemDetailsFactory.ProblemJsonMediaType },
            };
    }
}
