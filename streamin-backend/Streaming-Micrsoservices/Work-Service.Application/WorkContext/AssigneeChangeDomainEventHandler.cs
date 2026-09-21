using Contracts.WorkService.Events;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Work_Service.Application.Abstractions;
using Work_Service.Domain.WorkContext;
using Work_Service.Domain.WorkContext.WorkDomainEvents;
using ProjectUserClass = Work_Service.Domain.ProjectUser.ProjectUser;

namespace Work_Service.Application.WorkContext
{
    internal class AssigneeChangeDomainEventHandler:INotificationHandler<ChangeAssignmeeDomainEvent>
    {
        private readonly IUnitofWork<Workitem> _unitofWork;
        private readonly IUnitofWork<ProjectUserClass> _projectUserRepo;
        public AssigneeChangeDomainEventHandler(IUnitofWork<Workitem> unitofWork,IUnitofWork<ProjectUserClass> projectUserRepo )
        {
            _unitofWork = unitofWork;
            _projectUserRepo= projectUserRepo;
        }
        public async Task Handle(ChangeAssignmeeDomainEvent domainEvent, CancellationToken ct)
        {
            var OldAssigneeEntity = await _projectUserRepo.GetEntity(domainEvent.OldAssignedId);
            var newAssigneeEntity = await _projectUserRepo.GetEntity(domainEvent.NewAssignedId);
            string oldAssignee = OldAssigneeEntity.UserId.ToString();
            string newAssignee = OldAssigneeEntity.UserId.ToString();
            ChangeEvent changeEvent = new ChangeEvent(
                EventId: Guid.NewGuid(),
                occuredon: DateTime.UtcNow,
                workId: domainEvent.WorkId,
                WorkName: domainEvent.Name,
                ProjectId: domainEvent.ProjectId,
                ActorId: Guid.NewGuid(),
                AuditPayload: new AuditPayload(
                    comment: null,
                    change: new ChangeAudit(domainEvent.ChangedField, oldvalue: oldAssignee, newvalue: newAssignee)
                    )
                );
            _unitofWork.AddOutbox(changeEvent);
        }
    }
}
