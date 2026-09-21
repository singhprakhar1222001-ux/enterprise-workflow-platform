using Contracts;
using Contracts.WorkService.Consts;
using Contracts.WorkService.Events;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext;
using Work_Service.Domain.ProjectUser;
using Work_Service.Domain.WorkContext;
using WorkService.Persistance.Outbox;

namespace WorkService.Persistance.Interceptor
{
    public class EventInterceptor: SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {

            var context = eventData.Context;
            if(context is not IMediatRProvider)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }
            //var debugView = context.ChangeTracker.DebugView.LongView;
            //Console.WriteLine(debugView);
            var event_entity = context.ChangeTracker.Entries<Entity>();
            var ChangeList = event_entity.Where(e=>e.Entity.GetType()==typeof(Workitem)).Select(e => GetStateMap(e.Entity)).ToList();
            var changeWorkOutbox=new List<OutboxMessage>();
            foreach (var entry in ChangeList)
            {
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = entry.GetType().ToString(),
                    Message = JsonConvert.SerializeObject(entry),
                    IsError = false,
                    IsProcessed = false,
                    OccuredOn = DateTime.UtcNow
                };
                changeWorkOutbox.Add(outboxMessage);
            }
            //get work state change here



            var events = context.ChangeTracker
                .Entries<Entity>()
                .Select(e => e.Entity)
                .SelectMany(entity =>
                {
                    var DomainEvents = entity.getEvents();
                    entity.ClearContext();
                    return DomainEvents;
                }
                ).ToList();
            //only because efcore has access to scope that initialized it, usually we cannot access service from a dependency this way and we shouldn't
            
            
            foreach(var DomainEvent in events)
            {
                var appdbContext = (ApplicationbDbContext)context;
                await appdbContext._mediator.Publish(DomainEvent);
            }
            
            context?.AddRange(changeWorkOutbox);
            var res=await base.SavingChangesAsync(eventData, result, cancellationToken);
            return res;
        }

        private IIntegreationEvent? GetStateMap(Entity entity)
        {
           
            if(entity.GetType()==typeof(Workitem))
            {
                var w=(Workitem)entity;
                var list = new List<CommentEventProperty>();
                foreach(var domainComment in w._Comment)
                {
                    list.Add(new CommentEventProperty(
                        UserId: domainComment.Id.Value,
                       comment: domainComment.Comment,
                       Timestamp: domainComment.Timestamp
                        ));
                }
                var workstate= 
                    new WorkCreatedEvent(
                    eventID: Guid.NewGuid(),
                    Id: w.Id,
                    name: w.Name,
                    description: w.description,
                    comment: list,
                    ProjectId: w.ProjectId,
                    assignedId: w.assignedId,
                    managerId: w.managerId,
                    assignmentDate: w.AssignmentDate,
                    deadline: w.Deadline,
                    version:w.Version
                    );
                return workstate;
            }

            return null;

            
        }
        
    }
}
