using System;
using System.Collections.Generic;
using System.Text;
using Work_Service.Domain.Abstraction;

namespace Work_Service.Domain.ProjectUser.ProjectUserDomainEvents
{
    public class ChangeRoleDomainEvent(

        Guid Id,
        Guid EventId,
        Guid UserId,
        DateTime OccuredOn,
        Guid ProjectId,
        Role OldRole,
        Role NewRole
        ) : DomainEvents
    {
        public Guid Id { get; private set; } = Id;
        public Guid ProjectId { get; private set; } = ProjectId;
        public Guid EventId { get; private set; } = EventId;
        public Guid UserId { get; private set; } = UserId;
        public DateTime OccuredOn { get; private set; } = OccuredOn;

        public Role OldRole { get; private set; } = OldRole;
        public Role NewRole { get; private set; }= NewRole;

    }
}
