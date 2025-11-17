# EFCore.ModelExtras

A library that enables Entity Framework Core to track and migrate PostgreSQL
triggers and functions.

A chief goal of this project is that every database relation lives in your
codebase only once. Functions have one definition, and are referenced in
triggers via C# variables. In this way, navigating trigger invocations can
leverage your existing C# tooling rather than having to do complex database
introspection, or migration greping. Also, git diffs will work on your functions
and triggers, since they'll just be like any other code file instead of hiding
in a timestamp-identified half-codegened file!

> [!NOTE]\
> Disclosure: While I wrote the original foundations of this project, I've used
> AI _heavily_ to create documentation and to rearrange this into a publishable
> project, rather than just being written as internal tooling.

## Features

- **Track PostgreSQL Functions and Triggers**: Declare functions and triggers in
  your model and have them automatically created/updated via migrations
- **Triggers reference Functions with C# reference checks**: Trigger definitions
  reference the C# FunctionDefinition object, so you can both 1) get editor
  completions on your exisitng database functions and 2) get compile time errors
  if you remove a function that's in use
- **Git-Friendly**: Database procedural code is kept in dedicated source files
  instead of living principally in scattered migration files

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
        tb => tb
            .After
            .Update("email")
            .ForEachRow
            .Perform(MyDatabaseFunctions.Triggers.LogUserEmailChange);
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

```csharp
// 20251117013830_AddEmailChangeLogging.cs
migrationBuilder.Sql(/*lang=sql*/"""
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
    """);

migrationBuilder.Sql(/*lang=sql*/"""
    CREATE OR REPLACE TRIGGER tu_user_email_audit
        AFTER UPDATE OF email
        ON users
        FOR EACH ROW
        EXECUTE FUNCTION log_user_email_change()
    ;
    """);
```

### 5. Enabling migration change detection and formatting

To have the above items correctly migration-tracked and to get the formatted raw
string output shown above, add this design-time services class to your project:

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using EFCore.ModelExtras.Migrations;

/// <summary>
/// Configures EF Core's design-time code generation to use ModelExtras' custom
/// components. This class is automatically discovered by EF Core when running
/// commands like 'dotnet ef migrations add'.
/// </summary>
public class ModelExtrasDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // Detects changes to functions and triggers
        services.AddSingleton<IMigrationsModelDiffer, ModelExtrasModelDiffer>();

        // Generates C# migration code with pretty-formatted SQL strings
        services.AddSingleton<ICSharpMigrationOperationGenerator, ModelExtrasCSharpGenerator>();

        // Generates the actual SQL to execute at migration exec time
        services.AddSingleton<IMigrationsSqlGenerator, ModelExtrasSqlGenerator>();
    }
}
```

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
