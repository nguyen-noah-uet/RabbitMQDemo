using RabbitMQ.Client;
using Shared.Repositories;

namespace Api2.Services
{
    public class MessagePublisherService : IMessagePublisherService
    {
        private readonly IRabbitMqService _rabbitMqService;

        public MessagePublisherService(IRabbitMqService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService;
        }
        public void Publish<T>(T message, string exchangeName, string exchangeType, string queueName, string routeKey) where T : class
        {
            using var connection = _rabbitMqService.GetConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchangeName, exchangeType);
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.QueueBind(queueName, exchangeName, routeKey);
            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
            channel.BasicPublish(exchangeName, routeKey, null, body);
        }
    }

}
