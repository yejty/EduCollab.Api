using EduCollab.Api.Middleware;
using EduCollab.Api.Problems;
using EduCollab.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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
            var requestId = RequestIdMiddleware.GetRequestId(httpContext);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception while processing request {RequestId}.",
                    requestId);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Handled exception while processing request {RequestId}.",
                    requestId);
            }

            var detail = environment.IsDevelopment()
                ? $"{description} Details: {exception.Message}"
                : description;

            var problem = ApiProblemDetailsFactory.Create(httpContext, statusCode, error, detail);
            await ApiProblemDetailsWriter.WriteAsync(httpContext, problem, cancellationToken);
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
                PreconditionFailedException preconditionFailed => (
                    StatusCodes.Status412PreconditionFailed,
                    "precondition_failed",
                    preconditionFailed.Message),
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
                    "An unexpected error occurred."),
            };
    }
}
