using Amazon.SQS;
using Infra.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.DI
{
    public class InfraDependencies
    {
        public static void Inject(IServiceCollection services)
        {
            services.AddSingleton<IAmazonSQS, AmazonSQSClient>();
            services.AddSingleton<IMessageQueue, SqsMessageQueueWriter>();
        }
    }
}