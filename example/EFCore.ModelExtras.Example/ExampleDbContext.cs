using Microsoft.EntityFrameworkCore;

namespace EFCore.ModelExtras.Example;

public class ExampleDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailAuditLog> EmailAuditLogs => Set<EmailAuditLog>();

    public ExampleDbContext(DbContextOptions<ExampleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Declare the trigger functions in the model
        modelBuilder
            .DeclareFunction(DatabaseFunctions.LogUserEmailChange)
            .DeclareFunction(DatabaseFunctions.UpdateTimestamp);

        // Configure User entity
        modelBuilder.Entity<User>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Add trigger to log email changes
            entity.HasTrigger(
                "tu_user_email_audit",
                tb => tb
                    .After
                    .Update("email")
                    .ForEachRow
                    .Perform(DatabaseFunctions.LogUserEmailChange)
            );

            // Add trigger to update timestamp on any change
            entity.HasTrigger(
                "tu_user_update_timestamp",
                PgTriggerTiming.Before,
                PgTriggerEventClause.Update(),
                PgTriggerExecuteFor.EachRow,
                DatabaseFunctions.UpdateTimestamp
            );
        });

        // Configure EmailAuditLog entity
        modelBuilder.Entity<EmailAuditLog>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OldEmail).HasMaxLength(255);
            entity.Property(e => e.NewEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("NOW()");
        });
    }
}
