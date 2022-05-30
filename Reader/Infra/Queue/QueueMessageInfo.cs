using Domain;

namespace Infra.Queue
{
    public class QueueMessageInfo
    {
        public Message Message { get; set; }
        public string MessageQueueIdentifier { get; set; }

        public QueueMessageInfo(Message message, string messageQueueIdentifier)
        {
            Message = message;
            MessageQueueIdentifier = messageQueueIdentifier;
        }
    }
}