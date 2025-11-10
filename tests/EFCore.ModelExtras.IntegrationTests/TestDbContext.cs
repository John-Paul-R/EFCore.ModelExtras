using Microsoft.EntityFrameworkCore;

namespace EFCore.ModelExtras.IntegrationTests;

public class TestDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailAuditLog> EmailAuditLogs => Set<EmailAuditLog>();
    public DbSet<Post> Posts => Set<Post>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Declare functions
        modelBuilder
            .DeclareFunction(TestFunctions.LogUserEmailChange)
            .DeclareFunction(TestFunctions.UpdateTimestamp)
            .DeclareFunction(TestFunctions.ValidatePostContent);

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

        // Configure Post entity with triggers
        modelBuilder.Entity<Post>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Trigger to validate content length
            entity.HasTrigger(
                "tu_post_validate_content",
                PgTriggerTiming.Before,
                PgTriggerEventClause.Insert().Or(PgTriggerEventClause.Update("content")),
                PgTriggerExecuteFor.EachRow,
                TestFunctions.ValidatePostContent
            );
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

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
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

    public static readonly FunctionDeclaration ValidatePostContent = new(
        "validate_post_content",
        /*language=sql*/"""
        CREATE OR REPLACE FUNCTION validate_post_content()
          RETURNS trigger
          LANGUAGE plpgsql
        AS $function$
        BEGIN
            IF LENGTH(NEW.content) < 10 THEN
                RAISE EXCEPTION 'Post content must be at least 10 characters';
            END IF;
            RETURN NEW;
        END;
        $function$
        """
    );
}
