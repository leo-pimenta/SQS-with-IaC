using Domain;

namespace Infra.Queue
{
    public interface IMessageQueue 
    {
        Task<string> PushAsync(Message message);
    }
}