using EduCollab.Application.Exceptions;
using EduCollab.Contracts.Responses;
using Microsoft.AspNetCore.Diagnostics;

namespace EduCollab.Api.ExceptionHandlers
{
    public sealed class ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger,
        IHostEnvironment environment) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, error, description) = MapException(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception while processing request.");
            }
            else
            {
                logger.LogWarning(exception, "Handled exception while processing request.");
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Error = error,
                ErrorDescription = environment.IsDevelopment()
                    ? $"{description} Details: {exception.Message}"
                    : description
            };

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Error, string Description) MapException(Exception exception) =>
            exception switch
            {
                ArgumentException => (
                    StatusCodes.Status400BadRequest,
                    "bad_request",
                    "The request was invalid."),
                InvalidOperationException => (
                    StatusCodes.Status400BadRequest,
                    "bad_request",
                    "The request could not be completed."),
                UnauthorizedAccessException => (
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "Authentication is required for this operation."),
                AccessDeniedException => (
                    StatusCodes.Status403Forbidden,
                    "forbidden",
                    "You are not allowed to perform this operation."),
                KeyNotFoundException => (
                    StatusCodes.Status404NotFound,
                    "not_found",
                    "The requested resource was not found."),
                NotImplementedException => (
                    StatusCodes.Status501NotImplemented,
                    "not_implemented",
                    "This operation has not been implemented yet."),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "server_error",
                    "An unexpected error occurred.")
            };
    }
}
