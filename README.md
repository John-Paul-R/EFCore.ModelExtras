# EFCore.ModelExtras

A library that enables Entity Framework Core to track and migrate PostgreSQL
triggers and functions as part of your model, making database procedural code a
first-class citizen in your migrations.

> [!NOTE]\
> Disclosure: While I wrote the original foundations of this project, I've used
> AI _heavily_ to create documentation and to rearrange this into a publishable
> project, rather than just being written as internal tooling.

## Features

- **Track PostgreSQL Functions**: Declare functions in your model and have them
  automatically created/updated via migrations
- **Track PostgreSQL Triggers**: Define triggers with type-safe configuration
  and manage them through EF Core migrations
- **Git-Friendly**: Keep your database procedural code in dedicated source files
  instead of scattered across migration files
- **Automatic Migration Generation**: Changes to functions or triggers
  automatically appear in new migrations

## Installation

```bash
# NuGet package coming soon
# For now, include this project in your solution
```

## Getting Started

### 1. Enable Model Extras in Your DbContext

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseNpgsql(connectionString)
        .UseModelExtras();  // Enable triggers and functions support
}
```

### 2. Declare Functions

Create a static class to hold your function declarations:

```csharp
public static class MyDatabaseFunctions
{
    public static class Triggers
    {
        public static readonly FunctionDeclaration LogUserEmailChange = new(
            "log_user_email_change",
            /*language=sql*/"""
            CREATE OR REPLACE FUNCTION log_user_email_change()
              RETURNS trigger
              LANGUAGE plpgsql
            AS $function$
            BEGIN
                INSERT INTO email_audit_log (user_id, old_email, new_email, changed_at)
                VALUES (NEW.id, OLD.email, NEW.email, NOW());
                RETURN NEW;
            END;
            $function$
            """
        );
    }
}
```

Register functions in your `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder
        .DeclareFunction(MyDatabaseFunctions.Triggers.LogUserEmailChange);
}
```

### 3. Configure Triggers on Entities

Attach triggers to your entities using the fluent API:

```csharp
modelBuilder.Entity<User>(eb => {
    eb.HasTrigger(
        "tu_user_email_audit",
        PgTriggerTiming.After,
        PgTriggerEventClause.Update("email"),
        PgTriggerExecuteFor.EachRow,
        MyDatabaseFunctions.Triggers.LogUserEmailChange
    );
});
```

### 4. Generate and Apply Migrations

```bash
dotnet ef migrations add AddEmailAuditTrigger
dotnet ef database update
```

The generated migration will include SQL to create both the function and the
trigger!

## API Reference

### Declaring Functions

#### Simple Function Declaration

```csharp
modelBuilder.DeclareFunction(
    "my_function_name",
    "CREATE OR REPLACE FUNCTION my_function_name() ..."
);
```

#### Using FunctionDeclaration Objects

```csharp
public static readonly FunctionDeclaration MyFunction = new(
    "my_function",
    "CREATE OR REPLACE FUNCTION my_function() ..."
);

modelBuilder.DeclareFunction(MyFunction);
```

### Configuring Triggers

#### Basic Trigger with Custom SQL

```csharp
entityBuilder.HasTrigger(
    name: "my_trigger",
    triggerTiming: PgTriggerTiming.Before,
    triggerEvents: new[] {
        PgTriggerEventClause.Insert(),
        PgTriggerEventClause.Update()
    },
    source: "FOR EACH ROW EXECUTE FUNCTION my_function()"
);
```

#### Trigger Executing a Function

```csharp
entityBuilder.HasTrigger(
    name: "my_trigger",
    triggerTiming: PgTriggerTiming.After,
    triggerEvents: new[] { PgTriggerEventClause.Update("email", "phone") },
    executeFor: PgTriggerExecuteFor.EachRow,
    functionToExecute: MyDatabaseFunctions.MyTriggerFunction,
    when: "OLD.email IS DISTINCT FROM NEW.email"  // Optional condition
);
```

### Trigger Configuration Options

#### Trigger Timing

- `PgTriggerTiming.Before` - Execute before the operation
- `PgTriggerTiming.After` - Execute after the operation
- `PgTriggerTiming.InsteadOf` - Execute instead of the operation (for views)

#### Trigger Events

- `PgTriggerEventClause.Insert()` - Fire on INSERT
- `PgTriggerEventClause.Update()` - Fire on any UPDATE
- `PgTriggerEventClause.Update("col1", "col2")` - Fire only when specific
  columns are updated
- `PgTriggerEventClause.Delete()` - Fire on DELETE

#### Execute Mode

- `PgTriggerExecuteFor.Statement` - Execute once per statement
- `PgTriggerExecuteFor.EachRow` - Execute once per affected row

## Benefits Over Manual Migration SQL

### Before (Manual Approach)

```csharp
// Migration 20240101_Initial
migrationBuilder.Sql(@"
    CREATE FUNCTION log_changes() ...
");

// Migration 20240201_UpdateFunction
migrationBuilder.Sql(@"
    DROP FUNCTION log_changes;
    CREATE FUNCTION log_changes() ... -- Modified version
");

// Where's the current version? Need to check multiple migration files!
```

### After (Model Extras Approach)

```csharp
// Functions.cs - Single source of truth
public static readonly FunctionDeclaration LogChanges = new(
    "log_changes",
    "CREATE OR REPLACE FUNCTION log_changes() ... " // Latest version always here
);

// Migrations generated automatically when you change the function!
```

## Advanced Usage

### Function Overloads

```csharp
var calculateTax = new FunctionDeclaration(
    "calculate_tax",
    "CREATE FUNCTION calculate_tax(amount decimal) ...",
    OverloadDiscriminator: "_decimal"
);

var calculateTaxInt = new FunctionDeclaration(
    "calculate_tax",
    "CREATE FUNCTION calculate_tax(amount integer) ...",
    OverloadDiscriminator: "_int"
);
```

### Conditional Triggers

```csharp
entityBuilder.HasTrigger(
    "tu_audit_significant_changes",
    PgTriggerTiming.After,
    PgTriggerEventClause.Update(),
    PgTriggerExecuteFor.EachRow,
    MyFunctions.AuditChange,
    when: "NEW.amount > 1000 OR OLD.status != NEW.status"
);
```

## Requirements

- .NET 6.0 or higher
- Entity Framework Core 6.0 or higher
- Npgsql.EntityFrameworkCore.PostgreSQL 6.0 or higher

## License

MIT (see [LICENSE](LICENSE))

## Contributing

Contributions are welcome! This library is currently in development.
