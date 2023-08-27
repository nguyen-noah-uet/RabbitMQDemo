using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api2.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plain.RabbitMQ;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Shared.Common;
using Shared.Enums;
using Shared.Messages;
using Shared.Repositories;

namespace Api2.Listener
{
    public class S1MessageListener: BackgroundService
    {
        private const string S1ExchangeName = "s1.exchange";
        private const string S1QueueName = "s1.queue";
        private const string S1RoutingKey = "s1.routekey";
        private const string S1ExchangeType = RabbitMQ.Client.ExchangeType.Direct;
        private const string S2ExchangeName = "s2.exchange";
        private const string S2QueueName = "s2.queue";
        private const string S2RoutingKey = "s2.routekey";
        private const string S2ExchangeType = RabbitMQ.Client.ExchangeType.Direct;
        private readonly IMessagePublisherService _publisherService;

        private readonly IMessageConsumerService _consumerService;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        //private readonly ISubscriber _subscriber;
        //private readonly IServiceScopeFactory _serviceScopeFactory;
        //private readonly IPublisher _publisher;
        private IBookRepository _bookRepository = null!;
        private ILogger<S1MessageListener> _logger = null!;

        //public S1MessageListener(ISubscriber subscriber,IPublisher publisher, IServiceScopeFactory serviceScopeFactory)
        //{
        //    _subscriber = subscriber;
        //    _publisher = publisher;
        //    _serviceScopeFactory = serviceScopeFactory;
        //}
        public S1MessageListener(IMessagePublisherService publisherService, IMessageConsumerService consumerService, IServiceScopeFactory serviceScopeFactory)
        {
            _publisherService = publisherService;
            _consumerService = consumerService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://guest:guest@localhost:5672");
            factory.ClientProvidedName = "RabbitMQReceiver1";
            using IConnection connection = factory.CreateConnection();
            using IModel channel = connection.CreateModel();
            channel.ExchangeDeclare(S1ExchangeName, ExchangeType.Direct);
            channel.QueueDeclare(S1QueueName, false, false, false, null);
            channel.QueueBind(S1QueueName, S1ExchangeName, S1RoutingKey, null);
            channel.BasicQos(0, 1, false);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await OnS1SentMessage(JsonSerializer.Deserialize<S1UpdatedMessage>(message));
                channel.BasicAck(eventArgs.DeliveryTag, false);
            };
            while (!stoppingToken.IsCancellationRequested)
            {
                channel.BasicConsume(S1QueueName, false, consumer);
                await Task.Delay(1000, stoppingToken);
            }
            string consumerTag = channel.BasicConsume(S1QueueName, false, consumer);
            channel.BasicCancel(consumerTag);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("S1MessageListener started");
            _consumerService.StartConsuming<S1UpdatedMessage>(S1ExchangeName, S1QueueName, S1ExchangeType, S1RoutingKey, OnS1SentMessage);
            return Task.CompletedTask;
        }

        private async Task OnS1SentMessage(S1UpdatedMessage? arg)
        {
            if (arg == null)
            {
                return;
            }
            using var scope = _serviceScopeFactory.CreateScope();
            _bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<S1MessageListener>>();
            _logger.LogInformation("S1MessageListener received message");
            S1UpdatedMessage msg = arg;

            switch (msg.ActionType)
            {
                case ActionType.Add:
                    await AddBook(msg);
                    return;
                case ActionType.Update:
                    await UpdateBook(msg);
                    return;
                case ActionType.Delete:
                    await DeleteBook(msg);
                    return;
            }
        }

        //private async Task OnS1SendMessage<T>(T message)
        //{
        //    using var scope = _serviceScopeFactory.CreateScope();
        //    _bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
        //    _logger = scope.ServiceProvider.GetRequiredService<ILogger<S1MessageListener>>();
        //    _logger.LogInformation("S1MessageListener received message");
        //    S1UpdatedMessage? msg = JsonSerializer.Deserialize<S1UpdatedMessage>(message);
        //    if (msg == null)
        //    {
        //        _logger.LogError("Cannot deserialize object");
        //        return;
        //    }

        //    switch (msg.ActionType)
        //    {
        //        case ActionType.Add:
        //            await AddBook(msg);
        //            return;
        //        case ActionType.Update:
        //            await UpdateBook(msg);
        //            return;
        //        case ActionType.Delete:
        //            await DeleteBook(msg);
        //            return;
        //    }
        //}

        private async Task<bool> DeleteBook(S1UpdatedMessage msg)
        {
            _logger.LogInformation("Server 2 is deleting book");
            try
            {
                var book = await _bookRepository.DeleteBookAsync(msg.BookForUpdate.Id);
                _logger.LogInformation("Server 2 deleted book successfully");
                var response = new S2UpdatedResponse
                {
                    UpdatedBook = book,
                    ActionType = ActionType.Delete,
                    IsSuccess = true,
                    ErrorArgs = null
                };
                _logger.LogInformation("Server 2 is publishing message");
                _publisherService.Publish(response, S2ExchangeName, S2ExchangeType, S2QueueName, S2RoutingKey);
                _logger.LogInformation("Server 2 published message successfully");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                var response = new S2UpdatedResponse
                {
                    UpdatedBook = null,
                    ActionType = ActionType.Delete,
                    IsSuccess = false,
                    ErrorArgs = new ErrorArgs
                    {
                        ErrorMessage = e.Message,
                        OldValue = await _bookRepository.GetBookByIdAsync(msg.BookForUpdate.Id),
                        NewValue = null
                    }
                };
                _logger.LogInformation("Server 2 is publishing message");
                _publisherService.Publish(response, S2ExchangeName, S2ExchangeType, S2QueueName, S2RoutingKey);
                _logger.LogInformation("Server 2 published message successfully");
                return false;
            }
        }

        private async Task<bool> UpdateBook(S1UpdatedMessage msg)
        {
            _logger.LogInformation("Server 2 is updating book");
            try
            {
                var book = await _bookRepository.UpdateBookAsync(msg.BookForUpdate);
                _logger.LogInformation("Server 2 updated book successfully");
                var response = new S2UpdatedResponse
                {
                    UpdatedBook = book,
                    ActionType = ActionType.Update,
                    IsSuccess = true,
                    ErrorArgs = null
                };
                _logger.LogInformation("Server 2 is publishing message");
                _publisherService.Publish(response, S2ExchangeName, S2ExchangeType, S2QueueName, S2RoutingKey);
                _logger.LogInformation("Server 2 published message successfully");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                var response = new S2UpdatedResponse
                {
                    UpdatedBook = null,
                    ActionType = ActionType.Update,
                    IsSuccess = false,
                    ErrorArgs = new ErrorArgs
                    {
                        ErrorMessage = e.Message,
                        OldValue = await _bookRepository.GetBookByIdAsync(msg.BookForUpdate.Id),
                        NewValue = msg.BookForUpdate
                    }
                };
                _logger.LogInformation("Server 2 is publishing message");
                _publisherService.Publish(response, S2ExchangeName, S2ExchangeType, S2QueueName, S2RoutingKey);
                _logger.LogInformation("Server 2 published message successfully");
                return false;
            }
        }

        private async Task<bool> AddBook(S1UpdatedMessage msg)
        {
            _logger.LogInformation("Server 2 is adding book");
            try
            {
                var book = await _bookRepository.AddBookAsync(msg.BookForUpdate);
                _logger.LogInformation("Server 2 added book successfully");
                var response = new S2UpdatedResponse
                {
                    UpdatedBook = book,
                    ActionType = ActionType.Add,
                    IsSuccess = true,
                    ErrorArgs = null
                };
                _logger.LogInformation("Server 2 is publishing message");
                _publisherService.Publish(response, S2ExchangeName, S2ExchangeType, S2QueueName, S2RoutingKey);
                _logger.LogInformation("Server 2 published message successfully");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                var response = new S2UpdatedResponse
                {
                    UpdatedBook = null,
                    ActionType = ActionType.Add,
                    IsSuccess = false,
                    ErrorArgs = new ErrorArgs
                    {
                        ErrorMessage = e.Message,
                        OldValue = null,
                        NewValue = msg.BookForUpdate
                    }
                };
                _logger.LogInformation("Server 2 is publishing message");
                _publisherService.Publish(response, S2ExchangeName, S2ExchangeType, S2QueueName, S2RoutingKey);
                _logger.LogInformation("Server 2 published message successfully");
                return false;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("S1MessageListener stopped");
            return Task.CompletedTask;
        }
    }
}
