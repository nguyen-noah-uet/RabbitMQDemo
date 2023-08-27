using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api1.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plain.RabbitMQ;
using RabbitMQ.Client;
using Shared.Enums;
using Shared.Messages;
using Shared.Repositories;

namespace Api1.Listeners
{
    public class S2ResponseListener : IHostedService
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
        private IBookRepository _bookRepository = null!;
        private ILogger<S2ResponseListener> _logger = null!;

        public S2ResponseListener(IMessagePublisherService publisherService, IMessageConsumerService consumerService, IServiceScopeFactory serviceScopeFactory)
        {
            _publisherService = publisherService;
            _consumerService = consumerService;
            _serviceScopeFactory = serviceScopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("S2ResponseListener started");
            _consumerService.StartConsuming<S2UpdatedResponse>(S2ExchangeName, S2QueueName, S2ExchangeType, S2RoutingKey, OnS2Response);
            //_logger.LogInformation("S2ResponseListener started");
            return Task.CompletedTask;
        }

        private async Task OnS2Response(S2UpdatedResponse arg)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            _bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<S2ResponseListener>>();
            _logger.LogInformation("S2ResponseListener received message");
            S2UpdatedResponse? response = arg;
            if (response == null)
            {
                _logger.LogError("Cannot deserialize object");
                return;
            }

            if (response.IsSuccess)
            {
                _logger.LogInformation("Server 2 updated successfully");
                return;
            }
            _logger.LogWarning($"Server 2 failed to update with message {response.ErrorArgs?.ErrorMessage}. Rolling back...");
            var book = response.UpdatedBook;
            switch (response.ActionType)
            {
                case ActionType.Add:
                    _logger.LogInformation("Server 1 is rolling back for add book");
                    if (response.ErrorArgs == null)
                    {
                        _logger.LogError("ErrorArgs is null. Server 1 roll back failed");
                        return ;
                    }
                    var deleteBook = await _bookRepository.DeleteBookAsync(response.ErrorArgs.NewValue.Id);
                    if (deleteBook == null)
                    {
                        _logger.LogError("Failed to delete book. Server 1 roll back failed");
                        return ;
                    }
                    break;
                case ActionType.Update:
                    _logger.LogInformation("Server 1 is rolling back for update book");
                    if (response.ErrorArgs == null)
                    {
                        _logger.LogError("ErrorArgs is null. Server 1 roll back failed");
                        return ;
                    }
                    var oldBook = response.ErrorArgs.OldValue;
                    await _bookRepository.UpdateBookAsync(oldBook!);
                    break;
                case ActionType.Delete:
                    _logger.LogInformation("Server 1 is rolling back for delete book");
                    await _bookRepository.AddBookAsync(book!);
                    break;
            }
            _logger.LogInformation("Server 1 rolled back successfully");
            return ;
        }

        private async Task<bool> OnS2Response111(string message, IDictionary<string, object> header)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            _bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
            _logger = scope.ServiceProvider.GetRequiredService<Logger<S2ResponseListener>>();
            _logger.LogInformation("S2ResponseListener received message");
            S2UpdatedResponse? response = JsonSerializer.Deserialize<S2UpdatedResponse>(message);
            if (response == null)
            {
                _logger.LogError("Cannot deserialize object");
                return false;
            }

            if (response.IsSuccess)
            {
                _logger.LogInformation("Server 2 updated successfully");
                return true;
            }
            _logger.LogWarning($"Server 2 failed to update with message {response.ErrorArgs?.ErrorMessage}. Rolling back...");
            var book = response.UpdatedBook;
            switch (response.ActionType)
            {
                case ActionType.Add:
                    _logger.LogInformation("Server 1 is rolling back for add book");
                    if (response.ErrorArgs == null)
                    {
                        _logger.LogError("ErrorArgs is null. Server 1 roll back failed");
                        return false;
                    }
                    var deleteBook = await _bookRepository.DeleteBookAsync(response.ErrorArgs.NewValue.Id);
                    if (deleteBook == null)
                    {
                        _logger.LogError("Failed to delete book. Server 1 roll back failed");
                        return false;
                    }
                    break;
                case ActionType.Update:
                    _logger.LogInformation("Server 1 is rolling back for update book");
                    if (response.ErrorArgs == null)
                    {
                        _logger.LogError("ErrorArgs is null. Server 1 roll back failed");
                        return false;
                    }
                    var oldBook = response.ErrorArgs.OldValue;
                    await _bookRepository.UpdateBookAsync(oldBook!);
                    break;
                case ActionType.Delete:
                    _logger.LogInformation("Server 1 is rolling back for delete book");
                    await _bookRepository.AddBookAsync(book!);
                    break;
            }
            _logger.LogInformation("Server 1 rolled back successfully");
            return true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("S2ResponseListener stopped");
            return Task.CompletedTask;
        }
    }
}
