using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using EFCore.ModelExtras.Migrations;

namespace EFCore.ModelExtras.Example;

/// <summary>
/// Design-time services for Model Extras to enable custom migration code generation.
/// This class is automatically discovered by EF Core at design-time.
/// </summary>
public class ModelExtrasDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.AddSingleton<ICSharpMigrationOperationGenerator, ModelExtrasCSharpGenerator>();
    }
}
