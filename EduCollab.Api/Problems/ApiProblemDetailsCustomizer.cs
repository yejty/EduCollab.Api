using EduCollab.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EduCollab.Api.Problems
{
    public static class ApiProblemDetailsCustomizer
    {
        public static void Apply(HttpContext httpContext, ProblemDetails problem)
        {
            problem.Extensions.TryAdd("requestId", RequestIdMiddleware.GetRequestId(httpContext));

            if (!IsValidationProblem(problem))
            {
                return;
            }

            problem.Type = "urn:educollab:error:validation_failed";
            problem.Title = "Validation Failed";
            problem.Detail = "One or more validation errors occurred.";
            problem.Instance = httpContext.Request.Path;
            problem.Extensions["error"] = "validation_failed";

            if (problem is HttpValidationProblemDetails validationProblem)
            {
                NormalizeValidationErrorKeys(validationProblem);
            }
        }

        public static void NormalizeValidationErrorKeys(HttpValidationProblemDetails validationProblem)
        {
            if (validationProblem.Errors.Count == 0)
            {
                return;
            }

            var normalized = validationProblem.Errors
                .ToDictionary(
                    kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key),
                    kvp => kvp.Value,
                    StringComparer.Ordinal);

            validationProblem.Errors.Clear();
            foreach (var (key, value) in normalized)
            {
                validationProblem.Errors[key] = value;
            }
        }

        private static bool IsValidationProblem(ProblemDetails problem) =>
            problem is HttpValidationProblemDetails or ValidationProblemDetails;
    }
}
