using System.Net;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Infra.Exceptions;
using Infra.Validations;

namespace Infra.Queue
{
    public interface IMessageQueue
    {
        Task<IEnumerable<QueueMessageInfo>> PollMessagesAsync(int count);
        Task DeleteMessagesAsync(IEnumerable<QueueMessageInfo> messageInfos);
    }

    internal class SqsMessageQueueReader : IMessageQueue
    {
        private readonly IAmazonSQS SQS;
        private readonly TimeSpan VisibilityTimeout;
        private readonly TimeSpan WaitTimeSeconds;

        public SqsMessageQueueReader(IAmazonSQS sqs)
        {
            SQS = sqs;
            VisibilityTimeout = TimeSpan.FromMinutes(10);
            WaitTimeSeconds = TimeSpan.FromSeconds(5);
        }

        public async Task<IEnumerable<QueueMessageInfo>> PollMessagesAsync(int count)
        {
            Validate.This<ArgumentException>(count > 0, "Count has to be greater than 1.");

            var request = new ReceiveMessageRequest
            {
                QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL"),
                AttributeNames = new List<string> { "All" },
                MaxNumberOfMessages = count,
                VisibilityTimeout = Convert.ToInt32(Environment.GetEnvironmentVariable("SQS_QUEUE_VISIBILITY_TIMEOUT")),
                WaitTimeSeconds = Convert.ToInt32(Environment.GetEnvironmentVariable("SQS_QUEUE_WAIT_TIME_SECONDS")),
            };

            var response = await SQS.ReceiveMessageAsync(request);
            Validate.This<MessageReadException>(response.HttpStatusCode == HttpStatusCode.OK, 
                "Failed to read messages from SQS queue.");

            return response.Messages.Select(CreateMessageInfo);
        }

        public async Task DeleteMessagesAsync(IEnumerable<QueueMessageInfo> messageInfos)
        {
            Validate.This<ArgumentException>(messageInfos?.Any() ?? false, 
                "messageInfos must to contain at least one entry.");

            var deleteRequest = new DeleteMessageBatchRequest
            {
                QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL"),
                Entries = messageInfos.Select(CreateBatchDeleteEntry).ToList()
            }; 

            var response = await SQS.DeleteMessageBatchAsync(deleteRequest);

            foreach (var failedDeletion in response.Failed)
            {
                Console.WriteLine($"Failed to delete message: {JsonSerializer.Serialize(failedDeletion)}");
            }
        }

        private DeleteMessageBatchRequestEntry CreateBatchDeleteEntry(QueueMessageInfo messageInfo) =>
            new DeleteMessageBatchRequestEntry(
                Guid.NewGuid().ToString(), 
                messageInfo.MessageQueueIdentifier);

        private QueueMessageInfo CreateMessageInfo(Amazon.SQS.Model.Message sqsMessage)
        {
            var message = JsonSerializer.Deserialize<Domain.Message>(sqsMessage.Body);

            if (message == null)
            {
                throw new MessageReadException($"Error while reading message. ID:{sqsMessage.MessageId}");
            }

            return new QueueMessageInfo(message, sqsMessage.ReceiptHandle);
        }
    }
}