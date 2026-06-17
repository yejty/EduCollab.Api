namespace EduCollab.Application.Services.Users
{
    public interface IPlatformAdminAuthorization
    {
        Task EnsureCurrentUserIsPlatformAdminAsync(CancellationToken cancellationToken);
    }
}
