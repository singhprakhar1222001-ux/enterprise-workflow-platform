using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.Projections;

namespace Work_Service.Domain.ProjectUser.ProjectUserDomainEvents
{
    public class RemoveProjectUserDomainEvent(
        Guid Id,
        Guid EventId,
        Guid UserId,
        DateTime OccuredOn,
        Guid ProjectId
        ):DomainEvents
    {
        public Guid Id { get; private set; } = Id;
    public Guid ProjectId { get; private set; } = ProjectId;
    public Guid EventId { get; private set; } = EventId;
    public Guid UserId { get; private set; } = UserId;
    public DateTime OccuredOn { get; private set; } = OccuredOn;
}

}
