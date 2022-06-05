using System.Net;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Infra.Exceptions;
using Infra.Queue;
using Message = Domain.Message;

namespace Test.Infra
{
    public class SqsMessageQueueWriterTest
    {
        private readonly Mock<IAmazonSQS> SqsMock;
        private readonly SqsMessageQueueWriter QueueWriter;
        private readonly Queue<Message> MockQueue;

        public SqsMessageQueueWriterTest()
        {
            SqsMock = new Mock<IAmazonSQS>();
            QueueWriter = new SqsMessageQueueWriter(SqsMock.Object);
            MockQueue = new Queue<Message>();
        }
        
        [Fact]
        public async Task Should_SaveMessagesToQueue()
        {
            const int expectedCount = 15;
            MockSuccessEnqueue();
            var messages = CreateMessages(expectedCount);
            await CallPushAsyncToEachAsync(messages);

            MockQueue.Should().HaveCount(expectedCount);

            for (int i = 0; i < expectedCount; i++)
            {
                MockQueue.Dequeue().Should().BeEquivalentTo(messages[i]);
            }
        }

        [Fact]
        public async Task Should_Return_SavedMessageQueueId()
        {
            MockSuccessEnqueue();
            var message = new Message("Message body");
            var id = await QueueWriter.PushAsync(message);
            id.Should().BeEquivalentTo($"id1");
        }

        [Fact]
        public async Task Should_ThrowQueueWriteException_IfFailsToWriteToQeueue()
        {
            MockFailEnqueue();
            var message = new Message("Message body");
            await Assert.ThrowsAsync<QueueWriteException>(
                async () => await QueueWriter.PushAsync(message));
        }

        [Fact]
        public async Task Should_ThrowArgumentNullException_IfMessageIsNull()
        {
            #pragma warning disable CS8625
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await QueueWriter.PushAsync(null));
        }

        [Fact]
        public async Task Should_ThrowArgumentNullException_IfMessageBodyIsNull()
        {
            var message = new Message(null);
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await QueueWriter.PushAsync(message));
        }

        [Fact]
        public async Task Should_ThrowArgumentNullException_IfMessageBodyIsEmpty()
        {
            var message = new Message("");
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await QueueWriter.PushAsync(message));
        }

        [Fact]
        public async Task Should_ThrowArgumentNullException_IfMessageBodyIsOnlyWhitespaces()
        {
            var message = new Message("  ");
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await QueueWriter.PushAsync(message));
        }

        [Fact]
        public async Task Should_AlwaysSaveMessagesToSame_SqsGroup()
        {
            const int expectedCount = 15;
            MockSuccessEnqueue();
            var messages = CreateMessages(expectedCount);
            await CallPushAsyncToEachAsync(messages);
            
            SqsMock.Verify(sqs => sqs.SendMessageAsync(
                    It.Is<SendMessageRequest>(request => request.MessageGroupId.Equals("1")), 
                    It.IsAny<CancellationToken>()), 
                Times.Exactly(expectedCount));
            SqsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Should_AlwaysSendDifferentMessageDeduplicationId()
        {
            const int expectedCount = 15;
            var sentMessageDeduplicationId = new HashSet<string>();

            SqsMock.Setup(sqs => sqs.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns<SendMessageRequest, CancellationToken>(async (req, _) => 
                {
                    sentMessageDeduplicationId.Add(req.MessageDeduplicationId);
                    return await Task.FromResult(new SendMessageResponse()
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                    });
                });
            
            var messages = CreateMessages(expectedCount);
            await CallPushAsyncToEachAsync(messages);
            sentMessageDeduplicationId.Should().HaveCount(expectedCount);
        }

        private async Task CallPushAsyncToEachAsync(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                await QueueWriter.PushAsync(message);
            }
        }

        private void MockSuccessEnqueue()
        {
            SqsMock.Setup(sqs => sqs.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns<SendMessageRequest, CancellationToken>(async (req, _) => 
                {
                    var message = JsonSerializer.Deserialize<Message>(req.MessageBody);
                    MockQueue.Enqueue(message ?? throw new Exception("Test failed to deserialize"));
                    return await Task.FromResult(new SendMessageResponse()
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        MessageId = $"id{MockQueue.Count}"
                    });
                });
        }

        private void MockFailEnqueue()
        {
            SqsMock.Setup(sqs => sqs.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns<SendMessageRequest, CancellationToken>(async (req, b) => 
                {
                    return await Task.FromResult(new SendMessageResponse()
                    {
                        HttpStatusCode = HttpStatusCode.InternalServerError,
                    });
                });
        }

        private List<Message> CreateMessages(int count)
        {
            var messages = new List<Message>();

            for (int i = 1; i <= count; i++)
            {
                messages.Add(new Message($"Message body {i}"));
            }

            return messages;
        }
    }
}