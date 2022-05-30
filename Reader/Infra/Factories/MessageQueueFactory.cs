using Amazon.SQS;
using Infra.Queue;

namespace Infra.Factories
{
    public class MessageQueueFactory
    {
        public static IMessageQueue Create() => new SqsMessageQueueReader(new AmazonSQSClient());
    }
}