using Contracts.WorkService.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Text;

namespace Contracts.WorkService.RoutingEventDirectory
{
    public static class RoutingEventDirectory
    {
        private static readonly Dictionary<Type, string> RoutingKeys = new()
        {
            [typeof(WorkCreatedEvent)] = "workservice.search.work.created",
            [typeof(ProjectCreatedEvent)]="workservice.search.project.created",
            [typeof(ProjectUserCreatedEvent)]="workservice.search.user.created",
            [typeof(ChangeEvent)]="workservice.audit.changeevent"
        };
        private static readonly Dictionary<string,Type> TypeInference=RoutingKeys.ToDictionary(x=>x.Value,x=>x.Key);

        public static string GetRoutingKey(Type typeVal)
        {
            return RoutingKeys[typeVal];
        }
        public static Type GetTypeInference(string routingKeyValue)
        {
            return TypeInference[routingKeyValue];
        }
}
}
