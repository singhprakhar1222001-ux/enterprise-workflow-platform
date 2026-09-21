using Contracts.WorkService.Events;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using Work_Service.Application.Abstractions;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext;
using Work_Service.Domain.ProjectContext.DomainEventss;
using Work_Service.Domain.Projections;
using Work_Service.Domain.ProjectUser.ProjectUserDomainEvents;
using ProjectUserClass= Work_Service.Domain.ProjectUser.ProjectUser;

namespace Work_Service.Application.ProjectUser
{
    public record ProjectUserCommand(
        Guid UserId,
        Guid ProjectId,
        Role role
        ) : IRequest<ResponseObject<ProjectUserClass>>
    { }
    public class ProjectUserCommandHandler : IRequestHandler<ProjectUserCommand,ResponseObject<ProjectUserClass>>
    {
        private readonly IUnitofWork<ProjectUserClass> _ProjectUserUnitofWork;
        private readonly IUnitofWork<ProjectBase> _ProjectBaseUnitOfWork;
        private readonly IUnitofWork<User> _UserUnitOfWork;
        public ProjectUserCommandHandler(IUnitofWork<ProjectUserClass> ProjectUserUnitofWork,IUnitofWork<ProjectBase> ProjectBaseUnitOfWork,IUnitofWork<User> UserUnitOfWork)
        {
            _ProjectUserUnitofWork = ProjectUserUnitofWork;
            _ProjectBaseUnitOfWork = ProjectBaseUnitOfWork;
            _UserUnitOfWork = UserUnitOfWork;

        }

        public Task<ResponseObject<ProjectUserClass>> Handle(ProjectUserCommand request, CancellationToken cancellationToken)
        {
            if (_ProjectBaseUnitOfWork.GetEntity(request.ProjectId) == null)
            {
                ResponseObject<ProjectUserClass> res_fail = new ResponseObject<ProjectUserClass>(statusCode: 424, Error: true, ErrorMessage: "The assigned Project Doesnt exist",Response:null);
                return Task.FromResult(res_fail);
            }
            if (_UserUnitOfWork.GetEntity(request.UserId)==null){
                ResponseObject<ProjectUserClass> res_fail = new ResponseObject<ProjectUserClass>(statusCode: 424, Error: true, ErrorMessage: "The assigned User Doesnt exist",Response:null);
                return Task.FromResult(res_fail);
                
            }
            var ProjectUser = ProjectUserClass.AssignProjectUser(
                Userid:request.UserId,
                ProjectId:request.ProjectId,
                role:request.role
                );
            
            _ProjectUserUnitofWork.Add(ProjectUser);
            ResponseObject<ProjectUserClass> res = new ResponseObject<ProjectUserClass>(
                statusCode: 200, Error: false, Response: ProjectUser,ErrorMessage:null
                );
            return Task.FromResult(res);

        }
        
    }
    //when a user creates a project, we will have to react to that and to that effect we will have a user assignment with the main role
    //public class AssignCreatorHandler : INotificationHandler<ProjectCreatedDomainEvent>
    //{
    //    private readonly IUnitofWork<ProjectUserClass> unitofWork;
    //    public AssignCreatorHandler(IUnitofWork<ProjectUserClass> unitofWork)
    //    {
    //        this.unitofWork = unitofWork;
    //    }
    //    public async Task Handle(ProjectCreatedDomainEvent notification, CancellationToken cancellationToken)
    //    {
    //        var ProjectUser = ProjectUserClass.AssignProjectUser(
    //            Userid: notification.ProjectHead,
    //            ProjectId: notification.ProjectId,
    //            role: Role.Manager
    //            );

    //        unitofWork.Add(ProjectUser);

    //        return;
    //    }
    //}
    //MAJOR ANTI PATTERN IDENTIFIED IN ABOVE CASE,
    /*
     * In our outbox publishing code we have 
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
    For ProjectBaseCreated, this will cause loss of event for the ProjectCreated, but if we remove ToList that causes looping on a changing collection, strictly prohibited.
     */
    //Project User Created Event for Search Service
    public class ProjectUserCreatedDomainEventHandler : INotificationHandler<ProjectUserCreatedDomainEvent>
    {
        private IUnitofWork<ProjectUserClass> _unitOfWork;
        public ProjectUserCreatedDomainEventHandler(IUnitofWork<ProjectUserClass> unitofWork)
        {
            _unitOfWork= unitofWork;
        }
        public Task Handle(ProjectUserCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            var ProjectUserCreatedEvent = new ProjectUserCreatedEvent(
                Id: Guid.NewGuid(),
                EventId: notification.EventId,
                Name: notification.UserId.ToString(),
                OccuredOn: notification.OccuredOn
                );
            _unitOfWork.AddOutbox(ProjectUserCreatedEvent);
            return Task.CompletedTask;
        }
    }
}
