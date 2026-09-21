using Contracts.WorkService.Consts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.WorkService.Events
{
    public record WorkCreatedEvent(
        Guid eventID,
        Guid Id,
        string name,
        string description,
        List<CommentEventProperty>? comment,
        Guid ProjectId,
        Guid assignedId,
        Guid managerId,
        DateOnly assignmentDate,
        DateOnly deadline,
        int? version=null
        ) : IIntegreationEvent,INotification
    {
        public Guid EventId { get; set; } = eventID;

        public DateTime OccuredOn {  get; set; }=DateTime.UtcNow;

        public Guid Id { get; set; } = Id;
        public string Name { get; set; } = name;

        public Guid ProjectId { get; private set; } = ProjectId;
        public string description { get; private set; } = description;

        public List<CommentEventProperty> comment { get; private set; } = comment;

        public Guid assignedId { get; private set; }= assignedId;

        public Guid managerId { get; private set; }= managerId;

        public DateOnly AssignmentDate { get; private set; } = assignmentDate;

        public DateOnly Deadline { get; private set; } = deadline;
        public bool IsOverDue { get; private set; } = false;
        public WorkStatus WorkStatus { get; private set; } = WorkStatus.InProgress;
        public int Version { get; private set; } = version==null?1:version.Value;
    }
}
