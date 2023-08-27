using RabbitMQ.Client;

namespace Shared.Repositories
{
    public interface IRabbitMqService
    {
        public IConnection GetConnection();
    }
}
