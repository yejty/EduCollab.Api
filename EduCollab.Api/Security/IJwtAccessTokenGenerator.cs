namespace EduCollab.Api.Security
{
    public interface IJwtAccessTokenGenerator
    {
        string CreateAccessToken(int userId, string email);
    }
}
