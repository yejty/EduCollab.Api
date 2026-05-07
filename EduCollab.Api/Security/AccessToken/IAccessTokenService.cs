namespace EduCollab.Api.Security.AccessToken
{
    public interface IAccessTokenService
    {
        string Create(int userId, string email);
    }
}
