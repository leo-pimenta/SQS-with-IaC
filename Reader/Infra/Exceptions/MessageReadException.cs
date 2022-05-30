namespace Infra.Exceptions
{
    public class MessageReadException : Exception
    {
        public MessageReadException() {}
        public MessageReadException(string? message) : base(message) {}
        public MessageReadException(string? message, Exception? innerException)
            : base(message, innerException) {}
    }
}