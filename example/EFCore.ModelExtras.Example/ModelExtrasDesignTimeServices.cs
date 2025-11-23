using EFCore.ModelExtras.Core;
using EFCore.ModelExtras.Core.Generators;
using EFCore.ModelExtras.FunctionsAndTriggers;
using EFCore.ModelExtras.FunctionsAndTriggers.Migrations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.ModelExtras.Example;

/// <summary>
/// Design-time services for Model Extras to enable custom migration code generation.
/// This class is automatically discovered by EF Core at design-time.
/// </summary>
public class ModelExtrasDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // Register ModelExtras plugin with Core's extensibility system
        var options = new ModelExtrasOptions();
        options.AddPlugin(new ModelExtrasPlugin());
        services.AddSingleton<IModelExtrasRegistrationService>(
            new ModelExtrasRegistrationService(options));

        // Use Core's composite differ that orchestrates all registered differs
        services.AddSingleton<IMigrationsModelDiffer, CompositeModelDiffer>();

        // Generates C# migration code with pretty-formatted SQL strings
        services.AddSingleton<ICSharpMigrationOperationGenerator, PrettySqlCSharpGenerator>();

        // Generates the actual SQL to execute at migration exec time
        services.AddSingleton<IMigrationsSqlGenerator, ModelExtrasSqlGenerator>();
    }
}
