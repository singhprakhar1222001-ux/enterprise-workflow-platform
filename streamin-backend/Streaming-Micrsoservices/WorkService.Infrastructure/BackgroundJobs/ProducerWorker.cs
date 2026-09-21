using Contracts;
using Contracts.WorkService.RoutingEventDirectory;
using Contracts.WorkService.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WorkService.Infrastructure.Messages.Connection;
using WorkService.Infrastructure.Messages.Topology;
using WorkService.Persistance;
using WorkService.Persistance.Outbox;

namespace WorkService.Infrastructure.BackgroundJobs
{
    public class ProducerWorker:BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ProducerWorker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("PublishStarted");
                    using var scope = _scopeFactory.CreateScope();
                    var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationbDbContext>();
                    var connectionManager = scope.ServiceProvider.GetRequiredService<IConnectionManager>();
                    List<OutboxMessage> outboxMessages = dbcontext.OutboxMessages.Where(x => x.IsProcessed == false).Take(20).ToList();
                    var connection = await connectionManager.GetConnection();
                    var channel = await connection.CreateChannelAsync();
                    //get appropriate routing key
                    Assembly assembly = typeof(IIntegreationEvent).Assembly;
                    foreach (var outboxMessage in outboxMessages)
                    {
                        var MessageTypeString = outboxMessage.Type;
                        var type_message = assembly.GetType(MessageTypeString);
                        string routing_key = RoutingEventDirectory.GetRoutingKey(type_message);
                        var message = outboxMessage.Message;
                        byte[] messageByte = UTF8Encoding.UTF8.GetBytes(message);
                        await channel.BasicPublishAsync(
                            exchange: WorkExchangeTopology.WorkExchangeName,
                            routingKey: routing_key,
                            body: messageByte,
                            mandatory: true,
                            cancellationToken:stoppingToken
                            );
                        outboxMessage.IsProcessed = true;
                        await dbcontext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }
    }
}
