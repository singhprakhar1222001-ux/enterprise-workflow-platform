using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.WorkService.Events
{
    public record ChangeEvent(
        Guid EventId,
        DateTime occuredon,
        Guid? workId,
        string? WorkName,
        Guid ProjectId,
        Guid ActorId,
        AuditPayload? AuditPayload
        ) : IIntegreationEvent,INotification
    {
        public Guid EventId { get; init; } = EventId;

        public DateTime OccuredOn {  get; init; } = occuredon;

        public Guid? WorkId { get; init; }= workId;
        public string? WorkName { get; init; }= WorkName;
        public Guid ProjectId { get; init; }= ProjectId;
        public Guid ActorId { get; init; }= ActorId;
        public AuditPayload? AuditPayload { get; init; }= AuditPayload;
        
        
    }
    public record AuditPayload(
        string? comment,
        ChangeAudit? change
        );
    public record ChangeAudit(
        string? field,
        string? oldvalue,
        string? newvalue
    );
}
