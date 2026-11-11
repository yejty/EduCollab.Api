namespace Space.Application.Services.Users
{
    public interface IPlatformAdminAuthorization
    {
        Task EnsureCurrentUserIsPlatformAdminAsync(CancellationToken cancellationToken);
    }
}
