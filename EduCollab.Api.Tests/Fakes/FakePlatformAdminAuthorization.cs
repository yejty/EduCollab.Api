using EduCollab.Application.Exceptions;
using EduCollab.Application.Services.Users;
using Microsoft.AspNetCore.Http;

namespace EduCollab.Api.Tests.Fakes;

public sealed class FakePlatformAdminAuthorization : IPlatformAdminAuthorization
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FakePlatformAdminAuthorization(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public HashSet<int> PlatformAdminUserIds { get; } = [];

    public Task EnsureCurrentUserIsPlatformAdminAsync(CancellationToken cancellationToken)
    {
        var userIdHeader = _httpContextAccessor.HttpContext?.Request.Headers[TestAuthHandler.UserIdHeader].ToString();
        if (int.TryParse(userIdHeader, out var userId) && PlatformAdminUserIds.Contains(userId))
            return Task.CompletedTask;

        throw new AccessDeniedException("Insufficient rights.");
    }
}
