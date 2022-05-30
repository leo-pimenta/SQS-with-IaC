namespace Domain
{
    public class Message
    {
        public string Body { get; }

        public Message(string body)
        {
            Body = body;
        }
    }
}