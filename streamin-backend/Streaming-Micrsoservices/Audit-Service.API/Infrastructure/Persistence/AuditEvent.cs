namespace Audit_Service.API.Infrastructure.Persistence
{
    public class AuditEvent
    {
        
        public Guid EventId { get; set; }
        public Guid ActorId { get; set; }
        public Guid WorkId { get; set; }
        public string Properties { get; set; }
        public DateTime OccuredOn { get; set; }

    }
}
