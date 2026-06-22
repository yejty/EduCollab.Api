using Microsoft.AspNetCore.Mvc;

namespace EduCollab.Api.Problems
{
    public static class ApiProblemDetailsWriter
    {
        public static async Task WriteAsync(
            HttpContext httpContext,
            ProblemDetails problem,
            CancellationToken cancellationToken = default)
        {
            if (httpContext.Response.HasStarted)
            {
                return;
            }

            httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: ApiProblemDetailsFactory.ProblemJsonMediaType,
                cancellationToken: cancellationToken);
        }

        public static Task WriteAsync(
            HttpContext httpContext,
            int statusCode,
            string error,
            string detail,
            CancellationToken cancellationToken = default)
        {
            var problem = ApiProblemDetailsFactory.Create(httpContext, statusCode, error, detail);
            return WriteAsync(httpContext, problem, cancellationToken);
        }
    }
}
