using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Shared.Repositories;

namespace Api1.Services
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public RabbitMqService(IConfiguration configuration)
        {
            _host = configuration["RabbitMQ:Host"];
            _port = int.Parse(configuration["RabbitMQ:Port"]);
            _username = configuration["RabbitMQ:Username"];
            _password = configuration["RabbitMQ:Password"];
        }

        public IConnection GetConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _username,
                Password = _password
            };
            return factory.CreateConnection();
        }
    }
}
