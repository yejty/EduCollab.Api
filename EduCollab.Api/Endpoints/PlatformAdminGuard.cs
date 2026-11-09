using EduCollab.Application.Exceptions;
using EduCollab.Application.Services.Users;

namespace EduCollab.Api.MinimalApis;

internal static class PlatformAdminGuard
{
    public static async Task<IResult?> RequireAsync(
        HttpContext httpContext,
        IPlatformAdminAuthorization authorization,
        CancellationToken cancellationToken)
    {
        try
        {
            await authorization.EnsureCurrentUserIsPlatformAdminAsync(cancellationToken);
            return null;
        }
        catch (AccessDeniedException ex)
        {
            return ApiResults.Forbidden(httpContext, "forbidden", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ApiResults.Unauthorized(httpContext, "unauthorized", ex.Message);
        }
    }
}
