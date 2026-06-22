using EduCollab.Api.Problems;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EduCollab.Api.Security
{
    public static class JwtBearerProblemDetailsEvents
    {
        public static JwtBearerEvents Create() => new()
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                await ApiProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "Authentication is required for this operation.",
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                await ApiProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "forbidden",
                    "You are not allowed to perform this operation.",
                    context.HttpContext.RequestAborted);
            },
        };
    }
}
