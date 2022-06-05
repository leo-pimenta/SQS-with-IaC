using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Infra.Exceptions;
using Infra.Queue;

namespace Test.Infra
{
    public class SqsMessageQueueReaderTest
    {
        private const string SQS_EXPECTED_URL = "http://testhost/queue";
        private const int SQS_EXPECTED_VISIBILITY_TIMEOUT = 1030;
        private const int SQS_EXPECTED_WAIT_TIME_SECONDS = 1040;
        private const int POLL_EXPECTED_COUNT = 5;

        private readonly Mock<IAmazonSQS> SqsMock;
        private readonly SqsMessageQueueReader Reader;
        private List<Message> MessagesEnqueued;

        public SqsMessageQueueReaderTest()
        {
            this.MessagesEnqueued = new List<Message>();
            PopulateEnqueuedMessages(POLL_EXPECTED_COUNT);
            this.SqsMock = new Mock<IAmazonSQS>();
            MockDefaultBehavior();
            SetEnvironmentVariables();
            this.Reader = new SqsMessageQueueReader(this.SqsMock.Object);
        }

        [Fact]
        public async Task Should_ThrowMessageReadException_IfStatusCodeReturnedIsNotOk()
        {
            this.SqsMock.Setup(sqs => sqs.ReceiveMessageAsync(
                    It.IsAny<ReceiveMessageRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new ReceiveMessageResponse()
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                });

            await Assert.ThrowsAsync<MessageReadException>(async () => await this.Reader.PollMessagesAsync(5));
        }

        [Fact]
        public async Task Should_ReturnEmptyMessageEnumerable_IfQueueIsEmpty()
        {
            this.MessagesEnqueued.Clear();
            var messages = await this.Reader.PollMessagesAsync(POLL_EXPECTED_COUNT);
            messages.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_ReturnEnqueuedMessages()
        {
            int ExpectedMessagesCount = MessagesEnqueued.Count;
            var messages = await this.Reader.PollMessagesAsync(ExpectedMessagesCount);
            messages.Should().HaveCount(ExpectedMessagesCount);
        }

        [Fact]
        public async Task Should_SendCorrectReadRequestToSQS()
        {
            var expectedRequest = new ReceiveMessageRequest
            {
                QueueUrl = SQS_EXPECTED_URL,
                AttributeNames = new List<string> { "All" },
                MaxNumberOfMessages = POLL_EXPECTED_COUNT,
                VisibilityTimeout = SQS_EXPECTED_VISIBILITY_TIMEOUT,
                WaitTimeSeconds = SQS_EXPECTED_WAIT_TIME_SECONDS,
            };

            await this.Reader.PollMessagesAsync(POLL_EXPECTED_COUNT);

            this.SqsMock.Verify(sqs => sqs.ReceiveMessageAsync(
                It.Is<ReceiveMessageRequest>(request => 
                    request.QueueUrl == expectedRequest.QueueUrl
                    && request.AttributeNames.Count == 1
                    && request.AttributeNames.First() == expectedRequest.AttributeNames.First()
                    && request.MaxNumberOfMessages == expectedRequest.MaxNumberOfMessages
                    && request.WaitTimeSeconds == expectedRequest.WaitTimeSeconds
                ), 
                It.IsAny<CancellationToken>()), Times.Once);
            
            this.SqsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Should_SendCorrectDeleteRequestToSQS()
        {
            PopulateEnqueuedMessages(2);
            var messages = (await this.Reader.PollMessagesAsync(2)).ToList();

            var expectedRequest = new DeleteMessageBatchRequest
            {
                QueueUrl = SQS_EXPECTED_URL,
                Entries = new List<DeleteMessageBatchRequestEntry>
                {
                    new DeleteMessageBatchRequestEntry("", messages[0].MessageQueueIdentifier),
                    new DeleteMessageBatchRequestEntry("", messages[1].MessageQueueIdentifier)
                }
            };

            await this.Reader.DeleteMessagesAsync(messages);

            this.SqsMock.Verify(sqs => sqs.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            this.SqsMock.Verify(sqs => sqs.DeleteMessageBatchAsync(
                It.Is<DeleteMessageBatchRequest>(request => 
                    request.QueueUrl == expectedRequest.QueueUrl
                    && request.Entries.Count == 2
                    && request.Entries[0].Id.Length > 3
                    && request.Entries[0].ReceiptHandle == expectedRequest.Entries[0].ReceiptHandle
                    && request.Entries[1].Id.Length > 3
                    && request.Entries[1].ReceiptHandle == expectedRequest.Entries[1].ReceiptHandle
                ), 
                It.IsAny<CancellationToken>()), Times.Once);
            this.SqsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Should_ThrowArgumentExcpetion_On_PollMessagesAsync_WhenCountIsZero()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await this.Reader.PollMessagesAsync(0));
        }

        [Fact]
        public async Task Should_ThrowArgumentExcpetion_On_PollMessagesAsync_WhenCountIsNegative()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await this.Reader.PollMessagesAsync(new Random().Next(-9999, -1)));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_On_DeleteMessagesAsync_WhenMessageInfosIsNull()
        {
            #pragma warning disable CS8625
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await this.Reader.DeleteMessagesAsync(null));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_On_DeleteMessagesAsync_WhenMessageInfosIsEmpty()
        {
            #pragma warning disable CS8625
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await this.Reader.DeleteMessagesAsync(new List<QueueMessageInfo>()));
        }

        [Fact]
        // there's a possibly interesting comment about this on the SqsMessageQueueReader DeleteMessagesAsync method
        public async Task Should_SilentlyIgnoreDeletionErros()
        {
            this.SqsMock.Setup(sqs => sqs.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new DeleteMessageBatchResponse()
                {
                    Failed = new List<BatchResultErrorEntry>()
                    {
                        new BatchResultErrorEntry()
                    }
                }));

            var messageInfos = new List<QueueMessageInfo>()
            {
                new QueueMessageInfo(new Domain.Message(""), "")
            };

            try
            {
                await Reader.DeleteMessagesAsync(messageInfos);
            }
            catch
            {
                Assert.True(false);
            }
        }

        private void MockDefaultBehavior()
        {
            this.SqsMock.Setup(sqs => sqs.ReceiveMessageAsync(
                    It.IsAny<ReceiveMessageRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new ReceiveMessageResponse()
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Messages = this.MessagesEnqueued
                });
            
            this.SqsMock.Setup(sqs => sqs.DeleteMessageBatchAsync(
                It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()
            )).ReturnsAsync(() => new DeleteMessageBatchResponse()
            {
                HttpStatusCode = HttpStatusCode.OK
            });
        }

        private void PopulateEnqueuedMessages(int count)
        {
            this.MessagesEnqueued = new List<Message>();

            for (int i = 0; i < count; i++)
            {
                this.MessagesEnqueued.Add(new Message() { Body = "{\"Body\":\"A\"}" });
            }
        }

        private void SetEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("SQS_QUEUE_URL", SQS_EXPECTED_URL);
            
            Environment.SetEnvironmentVariable("SQS_QUEUE_VISIBILITY_TIMEOUT", 
                SQS_EXPECTED_VISIBILITY_TIMEOUT.ToString());
            
            Environment.SetEnvironmentVariable("SQS_QUEUE_WAIT_TIME_SECONDS", 
                SQS_EXPECTED_WAIT_TIME_SECONDS.ToString());
        }
    }
}