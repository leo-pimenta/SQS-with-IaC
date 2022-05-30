namespace App.Services
{
    internal class ContinuousQueueReader
    {
        private const int ReadCount = 10;

        private readonly ITimer Timer;
        private readonly IMessageService MessageService;

        public ContinuousQueueReader(ITimer timer, IMessageService messagService)
        {
            Timer = timer;
            Timer.Callback = Read;
            MessageService = messagService;
        }

        public void Start()
        {
            Timer.Start();
        }

        public void Stop()
        {
            Timer.Stop();
        }

        private void Read()
        {
            var task = MessageService.GetMessagesAsync(5);
            task.Wait();
            var messageInfos = task.Result;

            if (!messageInfos.Any())
            {
                Console.WriteLine("No messages received.");
            }
            else
            {
                foreach (var messageInfo in messageInfos)
                {
                    Console.WriteLine("Message: " + messageInfo.Message.Body);
                }

                MessageService.DeleteMessagesAsync(messageInfos).Wait();
            }
        }
    }
}