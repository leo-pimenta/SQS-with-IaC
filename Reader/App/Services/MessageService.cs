using Infra.Queue;

namespace App.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<QueueMessageInfo>> GetMessagesAsync(int count);
        Task DeleteMessagesAsync(IEnumerable<QueueMessageInfo> messageInfos);
    }

    internal class MessageService : IMessageService
    {
        private readonly IMessageQueue MessageQueue;

        public MessageService(IMessageQueue messageQueue)
        {
            MessageQueue = messageQueue;
        }

        public async Task DeleteMessagesAsync(IEnumerable<QueueMessageInfo> messageInfos)
        {
            await MessageQueue.DeleteMessagesAsync(messageInfos);
        }

        public async Task<IEnumerable<QueueMessageInfo>> GetMessagesAsync(int count)
        {
            return await MessageQueue.PollMessagesAsync(count);
        }
    }
}