using Audit_Service.API.Features;
using Audit_Service.API.Infrastructure.BufferService;
using Audit_Service.API.Infrastructure.Messaging;
using Audit_Service.API.Infrastructure.Messaging.Connection;
using Audit_Service.API.Infrastructure.Messaging.Topology;
using System.Runtime.CompilerServices;

namespace Audit_Service.API
{
    public static class DependencyInjection
    {
        public static void AddDependency(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionManager,ConnectionManager>();
            services.AddHostedService<BufferWriterJob>();
            services.AddSingleton<ITopologyInitializer, TopologyInitializer>();
            services.AddHostedService<ConsumerJob>();
            services.AddScoped<AuditQuery>();
        }
    }
}
