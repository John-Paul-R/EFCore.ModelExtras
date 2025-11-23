using EFCore.ModelExtras.FunctionsAndTriggers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace EFCore.ModelExtras.Example;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 EFCore.ModelExtras Example - PostgreSQL Triggers & Functions");
        Console.WriteLine("================================================================\n");

        // Create and start PostgreSQL container using Testcontainers
        Console.WriteLine("📦 Starting PostgreSQL container...");
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("example_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await postgresContainer.StartAsync();
        var connectionString = postgresContainer.GetConnectionString();
        Console.WriteLine($"✅ PostgreSQL container started\n");

        try {
            // Create DbContext with ModelExtras enabled
            var optionsBuilder = new DbContextOptionsBuilder<ExampleDbContext>();
            optionsBuilder
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()  // Use PostgreSQL-idiomatic snake_case
                .UseModelExtras()  // ⭐ Enable triggers and functions support
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

            using var context = new ExampleDbContext(optionsBuilder.Options);

            // Create database schema (tables, functions, and triggers)
            Console.WriteLine("📝 Creating database schema (tables, functions, and triggers)...");
            await context.Database.EnsureCreatedAsync();
            Console.WriteLine("✅ Database schema created\n");

            // Demonstrate the functionality
            await DemonstrateTriggersAndFunctions(context);

            // Show what was created in the database
            await ShowDatabaseObjects(context);
        }
        finally {
            // Clean up
            Console.WriteLine("\n🧹 Stopping PostgreSQL container...");
            await postgresContainer.StopAsync();
            Console.WriteLine("✅ Container stopped");
        }
    }

    static async Task DemonstrateTriggersAndFunctions(ExampleDbContext context)
    {
        Console.WriteLine("🔍 Demonstrating Triggers and Functions");
        Console.WriteLine("----------------------------------------\n");

        // Create a user
        Console.WriteLine("1. Creating a new user...");
        var user = new User {
            Name = "Alice Johnson",
            Email = "alice@example.com"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ Created user: {user.Name} ({user.Email})");
        Console.WriteLine($"   📅 CreatedAt: {user.CreatedAt}");
        Console.WriteLine($"   📅 UpdatedAt: {user.UpdatedAt ?? DateTime.MinValue}\n");

        // Update the user's name (should update timestamp but not log email)
        Console.WriteLine("2. Updating user's name...");
        user.Name = "Alice Smith";
        await context.SaveChangesAsync();
        await context.Entry(user).ReloadAsync();
        Console.WriteLine($"   ✅ Updated name to: {user.Name}");
        Console.WriteLine($"   📅 UpdatedAt: {user.UpdatedAt} (trigger updated this!)\n");

        // Update the user's email (should log to audit table)
        Console.WriteLine("3. Updating user's email...");
        var oldEmail = user.Email;
        user.Email = "alice.smith@example.com";
        await context.SaveChangesAsync();
        await context.Entry(user).ReloadAsync();
        Console.WriteLine($"   ✅ Updated email from {oldEmail} to {user.Email}");
        Console.WriteLine($"   📅 UpdatedAt: {user.UpdatedAt}\n");

        // Check the audit log
        var auditLogs = await context.EmailAuditLogs.ToListAsync();
        Console.WriteLine($"4. Checking email audit log...");
        Console.WriteLine($"   📋 Found {auditLogs.Count} audit log entry(ies):");
        foreach (var log in auditLogs) {
            Console.WriteLine($"      • User {log.UserId}: {log.OldEmail} → {log.NewEmail} at {log.ChangedAt}");
        }
        Console.WriteLine("   ✅ Email change was logged by the trigger!\n");
    }

    static async Task ShowDatabaseObjects(ExampleDbContext context)
    {
        Console.WriteLine("📊 Database Objects Created by ModelExtras");
        Console.WriteLine("-------------------------------------------\n");

        // Show functions
        var functions = await context.Database.SqlQueryRaw<string>(
            """
            SELECT proname
            FROM pg_proc
            WHERE pronamespace = 'public'::regnamespace
            AND proname IN ('log_user_email_change', 'update_timestamp')
            ORDER BY proname
            """
        ).ToListAsync();

        Console.WriteLine($"🔧 Functions ({functions.Count}):");
        foreach (var func in functions) {
            Console.WriteLine($"   • {func}");
        }
        Console.WriteLine();

        // Show triggers
        var triggers = await context.Database.SqlQueryRaw<TriggerInfo>(
            """
            SELECT
                t.tgname as name,
                c.relname as table_name,
                p.proname as function_name
            FROM pg_trigger t
            JOIN pg_class c ON t.tgrelid = c.oid
            JOIN pg_proc p ON t.tgfoid = p.oid
            WHERE NOT t.tgisinternal
            AND c.relname = 'users'
            ORDER BY t.tgname
            """
        ).ToListAsync();

        Console.WriteLine($"⚡ Triggers ({triggers.Count}):");
        foreach (var trigger in triggers) {
            Console.WriteLine($"   • {trigger.Name} on {trigger.TableName} → calls {trigger.FunctionName}()");
        }
    }
}

// Helper class for querying trigger info
public class TriggerInfo
{
    public string Name { get; set; } = "";
    public string TableName { get; set; } = "";
    public string FunctionName { get; set; } = "";
}
