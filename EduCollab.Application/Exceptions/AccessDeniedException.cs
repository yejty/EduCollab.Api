namespace EduCollab.Application.Exceptions
{
    public sealed class AccessDeniedException : Exception
    {
        public AccessDeniedException(string message)
            : base(message)
        {
        }
    }
}
