using Contracts;
using Contracts.WorkService.RoutingEventDirectory;
using Elastic.Clients.Elasticsearch.Mapping;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SearchService.API.Infrastructure.Messaging.Connection;
using SearchService.API.Infrastructure.Messaging.Topology;
using SearchService.API.Infrastructure.Projections;
using SearchService.API.Infrastructure.Projections.Models;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SearchService.API.Infrastructure.Messaging.BackgroundJobs
{
    public class ConsumerJob:BackgroundService
    {
        private IConnectionManager connectionManager;
        private IServiceScopeFactory scopeFactory;
        public ConsumerJob(IConnectionManager connectionManager, IServiceScopeFactory scopeFactory)
        {
            this.connectionManager = connectionManager;
            this.scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = await connectionManager.GetConnection();
            var channel = await connection.CreateChannelAsync();

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, EventArgs) =>
            {
                //check if event has already occured
                
                var properties = EventArgs.BasicProperties;
                int currentRetryCount = 0;
                if (properties != null && properties.Headers != null && properties.Headers.ContainsKey("x-retry-count")) {
                    currentRetryCount = Convert.ToInt32(properties.Headers["x-retry-count"]);
                }
                using var scope = scopeFactory.CreateScope();//dont leak to other messages
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                //check if event has already occured first
                try
                {
                    
                    string routing_key = EventArgs.RoutingKey;
                    Type type = RoutingEventDirectory.GetTypeInference(routing_key);
                    byte[] body = EventArgs.Body.ToArray();
                    string body_string = Encoding.UTF8.GetString(body);
                    var responseBody = JsonConvert.DeserializeObject(body_string, type);

                    IIntegreationEvent eventTypeConverted = (IIntegreationEvent)responseBody;
                    if (await context.EventLogs.AnyAsync(x => x.EventId == eventTypeConverted.EventId))
                    {
                        return;
                    }
                    using var transcation = await context.Database.BeginTransactionAsync();
                    await mediator.Publish(responseBody);
                    context.EventLogs.Add(new EventLog{
                        EventId = eventTypeConverted.EventId,
                        OccuredOn = eventTypeConverted.OccuredOn,
                    });
                    await context.SaveChangesAsync();
                    await transcation.CommitAsync();
                    await ((AsyncEventingBasicConsumer)sender).Channel.BasicAckAsync(EventArgs.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    currentRetryCount++;
                    var newProperties = new BasicProperties {Headers=new Dictionary<string,object>()};
                    newProperties?.Headers?["x-retry-count"] = currentRetryCount;
                    if (properties.Headers != null)
                    {
                        foreach (var val in properties.Headers)
                        {
                            newProperties?.Headers?[val.Key] = val.Value;
                        }
                    }
                    await RetryFunction(channel, newProperties, currentRetryCount,EventArgs,stoppingToken);
                    await ((AsyncEventingBasicConsumer)sender).Channel.BasicAckAsync(EventArgs.DeliveryTag, multiple: false);
                }
            };
            await channel.BasicConsumeAsync(Topology.Topology.EventQueue, autoAck: false, consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);


        }

        private async Task RetryFunction(IChannel channel,BasicProperties properties,int attempt,BasicDeliverEventArgs eventArgs,CancellationToken cancellationToken)
        {
            string? retryExchange = attempt switch
            {
                1 => Topology.Topology.retryExchange30s,
                2 => Topology.Topology.retryExchange60s,
                3 => Topology.Topology.retryExchange90s,
                _ => null
            };
            if (retryExchange == null)
            {
                await publishToDLQ(channel, eventArgs, cancellationToken);
                return;
            }
            await channel.BasicPublishAsync(
                exchange: retryExchange,
                routingKey: eventArgs.RoutingKey,
                body: eventArgs.Body.ToArray(),
                basicProperties: properties,
                cancellationToken:cancellationToken,
                mandatory:true
                );
        }
        private async Task publishToDLQ(IChannel channel,BasicDeliverEventArgs eventArgs,CancellationToken cancellationToken)
        {
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: Topology.Topology.DLQqueue,
                body: eventArgs.Body.ToArray(),
                cancellationToken: cancellationToken,
                mandatory: true
                );
        }
    }
}

