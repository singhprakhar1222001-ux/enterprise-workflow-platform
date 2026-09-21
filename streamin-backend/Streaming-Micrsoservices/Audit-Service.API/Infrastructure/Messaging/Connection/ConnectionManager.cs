using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;

namespace Audit_Service.API.Infrastructure.Messaging.Connection
{
    public class ConnectionManager
        : IConnectionManager
    {
        private IConnection connection;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IConfiguration configuration;
        public ConnectionManager(IConfiguration configuration)
        {
            connection = null;
            this.configuration = configuration;
        }
        public async Task<IConnection> GetConnection()
        {

            if (connection is { IsOpen: true })
            {
                return connection;
            }
            var connectionString = configuration.GetConnectionString("rabbitmq");
            try
            {
                await _lock.WaitAsync();
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(connectionString);
                connection = await factory.CreateConnectionAsync();

            }
            finally
            {
                _lock.Release();
            }
            return connection;
        }

        
    }
}
