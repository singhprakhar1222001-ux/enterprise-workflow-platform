
using Microsoft.EntityFrameworkCore;

namespace Audit_Service.API.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):DbContext(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            var auditEntity = builder.Entity<AuditEvent>();
            auditEntity.HasKey(x => x.EventId);
            auditEntity.HasIndex(x => x.ActorId);
            auditEntity.HasIndex(x => x.WorkId);
            auditEntity.Property(x => x.Properties)
                .HasColumnType("jsonb");
            auditEntity.HasIndex(x => x.Properties)
                .HasMethod("gin");
        }
        public DbSet<AuditEvent> AuditEvents { get; set; }
    }
}
