using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Infra.Exceptions;
using System.Net;
using Infra.Validations;

namespace Infra.Queue
{
    internal class SqsMessageQueueWriter : IMessageQueue
    {
        private IAmazonSQS SQS;

        public SqsMessageQueueWriter(IAmazonSQS sqs)
        {
            SQS = sqs;
        }

        public async Task<string> PushAsync(Domain.Message message)
        {
            var request = new SendMessageRequest
            {
                MessageBody = JsonSerializer.Serialize(message),
                QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL")
            };

            var response = await SQS.SendMessageAsync(request);

            Validate.This<QueueWriteException>(response.HttpStatusCode != HttpStatusCode.OK, 
                "Failed to send message to SQS.");
            
            return response.MessageId;
        }
    }
}