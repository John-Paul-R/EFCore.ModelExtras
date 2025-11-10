# EFCore.ModelExtras Example

This example demonstrates how to use **EFCore.ModelExtras** to track and migrate PostgreSQL triggers and functions in Entity Framework Core.

## What This Example Shows

This console application demonstrates:

1. **Declaring database functions** in your model
2. **Configuring triggers** on entities
3. **Automatic migration generation** for triggers and functions
4. **Running the application** with a PostgreSQL container (using Testcontainers)

## The Demo Scenario

The example creates a simple user management system with:

- **User table** with email tracking
- **EmailAuditLog table** to record email changes
- **Trigger function** (`log_user_email_change`) that automatically logs email changes to the audit table
- **Timestamp trigger** (`update_timestamp`) that automatically updates the `UpdatedAt` field on any change

## Prerequisites

- .NET 8.0 SDK
- Docker (for Testcontainers to run PostgreSQL)

## Running the Example

```bash
cd example/EFCore.ModelExtras.Example
dotnet run
```

The application will:
1. Start a PostgreSQL container automatically
2. Apply migrations (creating tables, functions, and triggers)
3. Demonstrate the triggers working by:
   - Creating a user
   - Updating the user's name (shows timestamp trigger)
   - Updating the user's email (shows audit logging trigger)
4. Display the created database objects
5. Clean up and stop the container

## What to Look For

### Generated Migration

Check `Migrations/InitialCreate.cs` to see how EFCore.ModelExtras generates SQL for:
- Function declarations
- Trigger attachments

### Console Output

When you run the example, you'll see:
- ✅ Confirmation that functions were created
- ✅ Confirmation that triggers were created
- ✅ Demonstration of triggers executing automatically
- ✅ Audit log entries created by the trigger

## Key Code

### Declaring Functions (DatabaseFunctions.cs)

```csharp
public static readonly FunctionDeclaration LogUserEmailChange = new(
    "log_user_email_change",
    """
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
```

### Configuring Triggers (ExampleDbContext.cs)

```csharp
modelBuilder.Entity<User>(entity => {
    // Add trigger to log email changes
    entity.HasTrigger(
        "tu_user_email_audit",
        PgTriggerTiming.After,
        PgTriggerEventClause.Update("email"),
        PgTriggerExecuteFor.EachRow,
        DatabaseFunctions.LogUserEmailChange
    );
});
```

### Enabling ModelExtras (ExampleDbContextFactory.cs or Program.cs)

```csharp
optionsBuilder
    .UseNpgsql(connectionString)
    .UseModelExtras();  // ⭐ This enables the library
```

## No PostgreSQL Installation Required!

This example uses **Testcontainers** to automatically spin up a PostgreSQL instance in Docker. You don't need to install or configure PostgreSQL manually - just have Docker running!
