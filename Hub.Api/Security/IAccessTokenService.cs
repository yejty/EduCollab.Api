namespace Hub.Api.Security
{
    public interface IAccessTokenService
    {
        string Create(int userId, string email);
    }
}
