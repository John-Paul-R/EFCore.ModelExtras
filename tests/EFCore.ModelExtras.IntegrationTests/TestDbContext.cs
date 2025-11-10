using Microsoft.EntityFrameworkCore;

namespace EFCore.ModelExtras.IntegrationTests;

public class TestDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailAuditLog> EmailAuditLogs => Set<EmailAuditLog>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Declare functions
        modelBuilder
            .DeclareFunction(TestFunctions.LogUserEmailChange)
            .DeclareFunction(TestFunctions.UpdateTimestamp);

        // Configure User entity with triggers
        modelBuilder.Entity<User>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Trigger to log email changes
            entity.HasTrigger(
                "tu_user_email_audit",
                PgTriggerTiming.After,
                PgTriggerEventClause.Update("email"),
                PgTriggerExecuteFor.EachRow,
                TestFunctions.LogUserEmailChange
            );

            // Trigger to update timestamp
            entity.HasTrigger(
                "tu_user_update_timestamp",
                PgTriggerTiming.Before,
                PgTriggerEventClause.Update(),
                PgTriggerExecuteFor.EachRow,
                TestFunctions.UpdateTimestamp
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

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EmailAuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? OldEmail { get; set; }
    public string NewEmail { get; set; } = "";
    public DateTime ChangedAt { get; set; }
}

public static class TestFunctions
{
    public static readonly FunctionDeclaration LogUserEmailChange = new(
        "log_user_email_change",
        /*language=sql*/"""
        CREATE OR REPLACE FUNCTION log_user_email_change()
          RETURNS trigger
          LANGUAGE plpgsql
        AS $function$
        BEGIN
            IF (TG_OP = 'UPDATE' AND OLD.email IS DISTINCT FROM NEW.email) THEN
                INSERT INTO email_audit_logs (user_id, old_email, new_email, changed_at)
                VALUES (NEW.id, OLD.email, NEW.email, NOW());
            END IF;
            RETURN NEW;
        END;
        $function$
        """
    );

    public static readonly FunctionDeclaration UpdateTimestamp = new(
        "update_timestamp",
        /*language=sql*/"""
        CREATE OR REPLACE FUNCTION update_timestamp()
          RETURNS trigger
          LANGUAGE plpgsql
        AS $function$
        BEGIN
            NEW.updated_at = NOW();
            RETURN NEW;
        END;
        $function$
        """
    );
}
