using Hub.Api.Problems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Hub.Api.MinimalApis;

internal static class ApiResults
{
    public static IResult Ok(object? value = null) =>
        value is null ? Results.Ok() : Results.Ok(value);

    public static IResult Created(string location, object value) =>
        Results.Created(location, value);

    public static IResult NoContent() => Results.NoContent();

    public static IResult Status(int statusCode, object? value = null) =>
        value is null
            ? Results.StatusCode(statusCode)
            : Results.Json(value, statusCode: statusCode);

    public static IResult Text(string content, string contentType) =>
        Results.Text(content, contentType);

    public static IResult File(byte[] data, string contentType, string? fileDownloadName = null) =>
        Results.File(data, contentType, fileDownloadName);

    public static IResult Problem(HttpContext httpContext, int statusCode, string error, string detail)
    {
        var problem = ApiProblemDetailsFactory.Create(httpContext, statusCode, error, detail);
        return JsonProblem(problem);
    }

    public static IResult BadRequest(HttpContext httpContext, string error, string detail) =>
        Problem(httpContext, StatusCodes.Status400BadRequest, error, detail);

    public static IResult Unauthorized(HttpContext httpContext, string error, string detail) =>
        Problem(httpContext, StatusCodes.Status401Unauthorized, error, detail);

    public static IResult Forbidden(HttpContext httpContext, string error, string detail) =>
        Problem(httpContext, StatusCodes.Status403Forbidden, error, detail);

    public static IResult NotFound(
        HttpContext httpContext,
        string error = "not_found",
        string detail = "The requested resource was not found.") =>
        Problem(httpContext, StatusCodes.Status404NotFound, error, detail);

    public static IResult Validation(HttpContext httpContext, ModelStateDictionary modelState)
    {
        var problem = ApiProblemDetailsFactory.CreateValidationProblem(httpContext, modelState);
        return JsonProblem(problem);
    }

    private static IResult JsonProblem(ProblemDetails problem) =>
        Results.Json(
            problem,
            statusCode: problem.Status ?? StatusCodes.Status500InternalServerError,
            contentType: ApiProblemDetailsFactory.ProblemJsonMediaType);
}
