namespace Infra.Exceptions
{
    public class QueueWriteException : InfraException
    {
        public QueueWriteException(){}
        public QueueWriteException(string? message) : base(message) {}
        public QueueWriteException(string? message, Exception? innerException)
            : base(message, innerException) {}
    }
}