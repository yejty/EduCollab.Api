namespace EduCollab.Contracts.Responses.Users
{
    public class UserResponse
    {
        public long Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

    }
}
