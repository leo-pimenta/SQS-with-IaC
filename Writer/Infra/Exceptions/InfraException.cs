namespace Infra.Exceptions
{
    public class InfraException : Exception
    {
        public InfraException(){}
        public InfraException(string? message) : base(message) {}
        public InfraException(string? message, Exception? innerException)
            : base(message, innerException) {}
    }
}