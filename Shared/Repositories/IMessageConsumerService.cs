using System;
using System.Threading.Tasks;

namespace Shared.Repositories
{
    public interface IMessageConsumerService
    {
        void StartConsuming<T>(string exchangeName, string queueName, string exchangeType, string routeKey, Func<T, Task> callBack) where T : class;
    }
}
