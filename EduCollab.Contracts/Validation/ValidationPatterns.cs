namespace EduCollab.Contracts.Validation
{
    public static class ValidationPatterns
    {
        public const string Email =
            @"\A[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}\z";

        public const string Password =
            @"\A(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}\z";

        public const string EmailError =
            "Email address must contain '@' and a valid domain, for example user@example.com.";

        public const string PasswordError =
            "Password must be at least 8 characters and contain at least one capital letter, one number, and one special character.";
    }
}
