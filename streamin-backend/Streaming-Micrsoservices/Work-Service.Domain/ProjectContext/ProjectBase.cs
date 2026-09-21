using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext.DomainEventss;

namespace Work_Service.Domain.ProjectContext
{
    public class ProjectBase:Entity
    {
        public ProjectBase() { }
        private ProjectBase(Guid guid, string Name, string Description, Guid ProjectHead)
        {
            Id = guid;
            this.Name = Name;
            this.Description = Description;
            this.ProjectHead = ProjectHead;
        }
        public Guid Id { get;}
        public string Name { get; private set; }

        public string Description { get; private set; }

        public Guid ProjectHead { get;private set; }

        

        public static ProjectBase? CreateProject(string Name, string Description, Guid Projecthead)
        {
            if(Name.Length<7 || Name[0] >90 || Name[0]<65)
            {
                throw new UserCreationException("User validation failed");
            }
            Guid guid = Guid.NewGuid();
            ProjectBase project=new ProjectBase(guid, Name, Description, Projecthead);
            project.AddEvent(
                new ProjectCreatedDomainEvent(
                    ProjectId: project.Id,
                    Name: project.Name,
                    Description: project.Description,
                    ProjectHead: project.ProjectHead
                    )
                );
            return project;
        }
        public string ChangeName(string Name)
        {
            this.Name = Name;
            return "Name changed Successfully";
        }
        public string ChangeDescription(string Description)
        {
            this.Description = Description;
            return "Description Changed Successfully";
        }
        
        
    }

    //this is the role within the project specific, doesnt have anything to with the user from projection
    
    public record UserDetails(string Name, Role Role);

    public class UserCreationException : Exception
    {
        public UserCreationException(string message) : base(message)
        {

        }
    }
}
