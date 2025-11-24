using EFCore.ModelExtras.Core;
using EFCore.ModelExtras.FunctionsAndTriggers;
using EFCore.ModelExtras.FunctionsAndTriggers.Generators;
using EFCore.ModelExtras.FunctionsAndTriggers.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;

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
            .UseModelExtras(options => {
                options.AddPlugin(new FunctionsAndTriggersPlugin());
            })
            // Manually register generators for migration generation
            .ReplaceService<ICSharpMigrationOperationGenerator, PrettySqlCSharpGenerator>()
            .ReplaceService<IMigrationsSqlGenerator, ModelExtrasSqlGenerator>();

        return new ExampleDbContext(optionsBuilder.Options);
    }
}
