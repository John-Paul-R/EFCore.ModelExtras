using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;
using Xunit;

namespace EFCore.ModelExtras.IntegrationTests;

public class MigrationIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _connectionString = "";

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("test_db")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container != null) {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task Functions_AreCreated_WhenMigrationApplied()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        await context.Database.MigrateAsync();

        // Assert
        var functions = await context.Database
            .SqlQuery<string>($"""
                SELECT proname
                FROM pg_proc
                WHERE pronamespace = 'public'::regnamespace
                AND proname IN ('log_user_email_change', 'update_timestamp')
                """)
            .ToListAsync();

        Assert.Equal(2, functions.Count);
        Assert.Contains("log_user_email_change", functions);
        Assert.Contains("update_timestamp", functions);
    }

    [Fact]
    public async Task Triggers_AreCreated_WhenMigrationApplied()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        await context.Database.MigrateAsync();

        // Assert
        var triggers = await context.Database
            .SqlQuery<TriggerInfo>($"""
                SELECT
                    t.tgname as name,
                    c.relname as table_name,
                    p.proname as function_name
                FROM pg_trigger t
                JOIN pg_class c ON t.tgrelid = c.oid
                JOIN pg_proc p ON t.tgfoid = p.oid
                WHERE NOT t.tgisinternal
                AND c.relname = 'users'
                """)
            .ToListAsync();

        Assert.Equal(2, triggers.Count);

        var emailAuditTrigger = triggers.FirstOrDefault(t => t.Name == "tu_user_email_audit");
        Assert.NotNull(emailAuditTrigger);
        Assert.Equal("users", emailAuditTrigger.TableName);
        Assert.Equal("log_user_email_change", emailAuditTrigger.FunctionName);

        var timestampTrigger = triggers.FirstOrDefault(t => t.Name == "tu_user_update_timestamp");
        Assert.NotNull(timestampTrigger);
        Assert.Equal("users", timestampTrigger.TableName);
        Assert.Equal("update_timestamp", timestampTrigger.FunctionName);
    }

    [Fact]
    public async Task Functions_AreDropped_WhenMigrationReverted()
    {
        // Arrange
        using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Act - Revert to no migrations
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("0");

        // Assert
        var functions = await context.Database
            .SqlQuery<string>($"""
                SELECT proname
                FROM pg_proc
                WHERE pronamespace = 'public'::regnamespace
                AND proname IN ('log_user_email_change', 'update_timestamp')
                """)
            .ToListAsync();

        Assert.Empty(functions);
    }

    [Fact]
    public async Task Triggers_AreDropped_WhenMigrationReverted()
    {
        // Arrange
        using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Act - Revert to no migrations
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("0");

        // Assert
        // After reverting migrations, the table won't exist,
        // so we check pg_trigger directly without joining to the table
        var result = await context.Database
            .SqlQuery<CountResult>($"""
                SELECT COALESCE(COUNT(*), 0)::int as count
                FROM pg_trigger t
                JOIN pg_class c ON t.tgrelid = c.oid
                JOIN pg_proc p ON t.tgfoid = p.oid
                WHERE NOT t.tgisinternal
                AND c.relname = 'users'
                """)
            .FirstOrDefaultAsync();

        Assert.Equal(0, result?.Count ?? 0);
    }

    private TestDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention()
            .UseModelExtras();

        return new TestDbContext(optionsBuilder.Options);
    }

    private class TriggerInfo
    {
        public string Name { get; set; } = "";
        public string TableName { get; set; } = "";
        public string FunctionName { get; set; } = "";
    }

    private class CountResult
    {
        public int Count { get; set; }
    }
}
