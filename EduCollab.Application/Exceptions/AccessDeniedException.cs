namespace EduCollab.Application.Exceptions
{
    public sealed class AccessDeniedException : Exception
    {
        public AccessDeniedException(string message, string errorCode = "forbidden")
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
    }
}
