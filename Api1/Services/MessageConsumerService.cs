using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Repositories;

namespace Api1.Services
{
    public class MessageConsumerService : IMessageConsumerService
    {
        private readonly IRabbitMqService _rabbitMqService;

        public MessageConsumerService(IRabbitMqService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService;
        }
        public void StartConsuming<T>(string exchangeName, string queueName, string exchangeType, string routeKey, Func<T, Task> callBack) where T : class
        {
            using (var connection = _rabbitMqService.GetConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchangeName, exchangeType);
                channel.QueueDeclare(queueName, false, false, false, null);
                channel.QueueBind(queueName, exchangeName, routeKey, null);
                channel.BasicQos(0, 1, false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (sender, eventArgs) =>
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var messageObject = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(message);
                    await callBack(messageObject);
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                };

                string consumerTag = channel.BasicConsume(queueName, false, consumer);
                channel.BasicCancel(consumerTag);
            }
        }
    }
}
