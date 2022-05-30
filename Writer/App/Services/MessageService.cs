using Domain;
using Infra.Queue;
using Infra.Validations;

namespace App.Services
{
    public interface IMessageService
    {
        Task<string> SendAsync(Message message);
    }

    internal class MessageService : IMessageService
    {
        private IMessageQueue MessageQueue;

        internal MessageService(IMessageQueue messageQueue)
        {
            MessageQueue = messageQueue;
        }

        public async Task<string> SendAsync(Message message)
        {
            ValidateMessage(message);
            return await MessageQueue.PushAsync(message);
        }

        private void ValidateMessage(Message message)
        {
            Validate.This(message != null, "Message cannot be null.");
            Validate.This(!string.IsNullOrWhiteSpace(message.Body), "Message body cannot be null or an empty string.");
        }   
    }
}