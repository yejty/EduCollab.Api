namespace EduCollab.Application.Exceptions
{
    public sealed class PreconditionFailedException : Exception
    {
        public PreconditionFailedException(string message)
            : base(message)
        {
        }
    }
}
