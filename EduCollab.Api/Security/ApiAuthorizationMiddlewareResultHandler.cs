using EduCollab.Api.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace EduCollab.Api.Security
{
    public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Succeeded)
            {
                await next(context);
                return;
            }

            if (authorizeResult.Challenged)
            {
                await ApiProblemDetailsWriter.WriteAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "Authentication is required for this operation.",
                    context.RequestAborted);
                return;
            }

            if (authorizeResult.Forbidden)
            {
                await ApiProblemDetailsWriter.WriteAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "forbidden",
                    "You are not allowed to perform this operation.",
                    context.RequestAborted);
                return;
            }

            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
