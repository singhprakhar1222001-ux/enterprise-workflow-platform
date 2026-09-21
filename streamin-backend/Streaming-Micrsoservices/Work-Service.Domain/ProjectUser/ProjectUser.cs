using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext;
using Work_Service.Domain.Projections;
using Work_Service.Domain.ProjectUser.ProjectUserDomainEvents;

namespace Work_Service.Domain.ProjectUser
{
    
    public class ProjectUser:Entity
    {
        private ProjectUser() { }
        private ProjectUser(Guid UserId,Guid ProjectId ,Role role)
        {
            this.UserId = UserId;
            this.Role = role;
            this.ProjectId= ProjectId;
        }
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public Role Role { get; private set; }

        public Guid ProjectId { get; private set; }

        public static ProjectUser AssignProjectUser(Guid Userid,Guid ProjectId, Role role)
        {
            ProjectUser user = new ProjectUser(Userid,ProjectId, role);
            user.AddEvent(
                new ProjectUserCreatedDomainEvent(
                    Id: user.Id,
                    EventId:Guid.NewGuid(),
                    OccuredOn:DateTime.UtcNow,
                    UserId:user.UserId,
                    ProjectId:user.ProjectId
                    )
                );

            return user;
        }
        
        public void RemoveUser(Guid ProjectUserId)
        {
            this.AddEvent(
                new ProjectUserCreatedDomainEvent(
                    Id: this.Id,
                    EventId: Guid.NewGuid(),
                    OccuredOn: DateTime.UtcNow,
                    UserId: this.UserId,
                    ProjectId: this.ProjectId
                    )
                );
        }

        public void ChangeRole(Guid ProjectUserId,Guid Role)
        {

        }
        
    }
}
