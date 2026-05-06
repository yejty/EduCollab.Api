namespace EduCollab.Application.Models.Users
{
    public sealed class AuthenticatedUser
    {
        public AuthenticatedUser(int id, string email)
        {
            Id = id;
            Email = email;
        }

        public int Id { get; }

        public string Email { get; }
    }
}
