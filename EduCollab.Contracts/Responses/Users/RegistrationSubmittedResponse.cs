namespace EduCollab.Contracts.Responses.Users
{
    /// <summary>
    /// Returned after registration when an email confirmation message was sent.
    /// </summary>
    public sealed class RegistrationSubmittedResponse
    {
        public string Message { get; set; } =
            "Account created. Check your email to confirm your address before signing in.";
    }
}
