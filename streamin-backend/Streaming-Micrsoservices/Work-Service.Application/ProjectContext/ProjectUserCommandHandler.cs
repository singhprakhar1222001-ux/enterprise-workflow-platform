using Contracts.WorkService.Events;
using MediatR;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using Work_Service.Application.Abstractions;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext;
using Work_Service.Domain.ProjectContext.DomainEventss;
using ProjectUserClass=Work_Service.Domain.ProjectUser.ProjectUser;

namespace Work_Service.Application.ProjectContext
{
    public record ProjectCreateCommand(
        string Name,
        string Description,
        Guid ProjectHead
        ) : IRequest { }
    public class ProjectCreatedCommandHandler : IRequestHandler<ProjectCreateCommand>
    {
        private readonly IUnitofWork<ProjectBase> _unitOfWork;
        private readonly IUnitofWork<ProjectUserClass> _userUnitOfWork;
        public ProjectCreatedCommandHandler(IUnitofWork<ProjectBase> unitofWork,IUnitofWork<ProjectUserClass> userUnitOfWork)
        {
            _unitOfWork= unitofWork;
            _userUnitOfWork= userUnitOfWork;
        }
        public async Task Handle(ProjectCreateCommand request, CancellationToken cancellationToken)
        {
            var projectBase=ProjectBase.CreateProject(Name: request.Name, Description: request.Description, Projecthead: request.ProjectHead);
            _unitOfWork.Add(projectBase);
            var ProjectUser = ProjectUserClass.AssignProjectUser(
                Userid: projectBase.ProjectHead,
                ProjectId: projectBase.Id,
                role: Role.Manager
                );
            _userUnitOfWork.Add(ProjectUser);
            await _unitOfWork.SaveChangesAsync();
            return;
        }

        
    }
    public class ProjectCreatedDomainEventHandler : INotificationHandler<ProjectCreatedDomainEvent>
    {
        private readonly IUnitofWork<ProjectBase> _unitofWork;
        public ProjectCreatedDomainEventHandler(IUnitofWork<ProjectBase> unitofWork)
        {
            _unitofWork= unitofWork;
        }
        public async Task Handle(ProjectCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            ProjectCreatedEvent projectCreatedEvent = new ProjectCreatedEvent(
                EventId:Guid.NewGuid(),
                Id:domainEvent.ProjectId,
                ProjectName:domainEvent.Name,
                OccuredOn:DateTime.UtcNow
                );
            _unitofWork.AddOutbox(projectCreatedEvent);
            
        
        }
    }
    public class ProjectCreatedSecondHandler : INotificationHandler<ProjectCreatedDomainEvent>
    {
        private readonly IUnitofWork<ProjectBase> _unitofWork;
        public ProjectCreatedSecondHandler(IUnitofWork<ProjectBase> unitofWork)
        {
            _unitofWork = unitofWork;
        }
        public async Task Handle(ProjectCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var changeEvent = new ChangeEvent(
                EventId: Guid.NewGuid(),
                occuredon: DateTime.UtcNow,
                workId: null,
                WorkName: null,
                ProjectId: domainEvent.ProjectId,
                ActorId: domainEvent.ProjectHead,
                new AuditPayload(
                    comment: null,
                    change: new ChangeAudit(
                        field:"ProjectName",
                        oldvalue:null,
                        newvalue: domainEvent.Name
                        )
                    )
                );
            _unitofWork.AddOutbox(changeEvent);
        }
    }
}
