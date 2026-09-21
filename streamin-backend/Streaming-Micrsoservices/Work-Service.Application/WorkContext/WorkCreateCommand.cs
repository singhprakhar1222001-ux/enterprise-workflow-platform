using Contracts.WorkService.Consts;
using Contracts.WorkService.Events;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Work_Service.Application.Abstractions;
using Work_Service.Domain.WorkContext;
using Work_Service.Domain.WorkContext.WorkDomainEvents;

namespace Work_Service.Application.WorkContext
{
    public record WorkCreateCommand(
        string Name,
        string Description,
        string Comment,
        Guid ProjectId,
        Guid AssigneeId,
        Guid ManagerId,
        DateOnly Deadline
        ) : IRequest<Unit> { }

    public class WorkCreateCommandHandler : IRequestHandler<WorkCreateCommand, Unit>
    {
        private readonly IUnitofWork<Workitem> _unitofwork;
        public WorkCreateCommandHandler(IUnitofWork<Workitem> unitofWork) {
            _unitofwork = unitofWork;
        }
        public async Task<Unit> Handle(WorkCreateCommand request, CancellationToken cancellationToken)
        {
            Workitem workitem = Workitem.CreateWorkItem(
                Name: request.Name,
                description: request.Description,
                comment: request.Comment,
                ProjectId: request.ProjectId,
                assignedId: request.AssigneeId,
                Deadline: request.Deadline,
                managerId:request.ManagerId
                );


            _unitofwork.Add(workitem);
            await _unitofwork.SaveChangesAsync();
            return Unit.Value;
            
        }
    }

    public class WorkCreatedAuditEvent : INotificationHandler<WorkCreatedDomainEvent>
    {
        private readonly IUnitofWork<Workitem> _unitOfWork;
        public WorkCreatedAuditEvent(IUnitofWork<Workitem> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(WorkCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            ChangeEvent changeEvent = new ChangeEvent
            (
                EventId:notification.EventId,
                ActorId:Guid.NewGuid(),
                occuredon:DateTime.UtcNow,
                workId : notification.Id,
                WorkName : notification.Name,
                ProjectId : notification.ProjectId,
                AuditPayload : new AuditPayload(
                    comment: "",
                    change: new ChangeAudit(field: "projectId", oldvalue: "none", newvalue: notification.Id.ToString())
                )
            );
            _unitOfWork.AddOutbox(changeEvent);
        }
    }

}
