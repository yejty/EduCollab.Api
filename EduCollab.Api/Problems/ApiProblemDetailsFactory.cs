using EduCollab.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EduCollab.Api.Problems
{
    public static class ApiProblemDetailsFactory
    {
        public const string ProblemJsonMediaType = "application/problem+json";

        public static ProblemDetails Create(
            HttpContext httpContext,
            int statusCode,
            string error,
            string detail)
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = detail,
                Type = $"urn:educollab:error:{error}",
                Instance = httpContext.Request.Path,
            };

            problem.Extensions["error"] = error;
            problem.Extensions["requestId"] = RequestIdMiddleware.GetRequestId(httpContext);

            return problem;
        }

        public static ValidationProblemDetails CreateValidationProblem(
            HttpContext httpContext,
            ModelStateDictionary modelState)
        {
            var problem = new ValidationProblemDetails(modelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Type = "urn:educollab:error:validation_failed",
                Instance = httpContext.Request.Path,
            };

            problem.Extensions["error"] = "validation_failed";
            problem.Extensions["requestId"] = RequestIdMiddleware.GetRequestId(httpContext);

            ApiProblemDetailsCustomizer.NormalizeValidationErrorKeys(problem);

            return problem;
        }

        private static string GetTitle(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status412PreconditionFailed => "Precondition Failed",
            StatusCodes.Status406NotAcceptable => "Not Acceptable",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            StatusCodes.Status501NotImplemented => "Not Implemented",
            _ => "Error",
        };
    }
}
