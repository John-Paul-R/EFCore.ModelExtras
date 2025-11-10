using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EFCore.ModelExtras.Example;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// </summary>
public class ExampleDbContextFactory : IDesignTimeDbContextFactory<ExampleDbContext>
{
    public ExampleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExampleDbContext>();

        // Use a dummy connection string for design-time operations
        // The actual connection string will be provided at runtime
        optionsBuilder
            .UseNpgsql("Host=localhost;Database=example_design;Username=postgres;Password=postgres")
            .UseModelExtras();

        return new ExampleDbContext(optionsBuilder.Options);
    }
}
