namespace Shared.Repositories
{
    public interface IMessagePublisherService
    {
        void Publish<T>(T message, string exchangeName, string exchangeType, string queueName, string routeKey) where T : class;
    }
}