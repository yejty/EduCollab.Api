namespace EduCollab.Application.Identity
{
    /// <summary>
    /// Authenticated caller context (e.g. JWT claims). Not set when the request is anonymous.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Database user id from the access token, or null if unauthenticated or claim is missing/invalid.
        /// </summary>
        int? UserId { get; }
    }
}
