using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using EFCore.ModelExtras.Migrations;

namespace EFCore.ModelExtras;

/// <summary>
/// Extension methods for configuring Model Extras on a <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class ModelExtrasDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the context to use Model Extras for tracking and migrating PostgreSQL triggers and functions.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseModelExtras(this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IMigrationsModelDiffer, ModelExtrasModelDiffer>();
        optionsBuilder.ReplaceService<ICSharpMigrationOperationGenerator, ModelExtrasCSharpGenerator>();
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, ModelExtrasSqlGenerator>();
        return optionsBuilder;
    }

    /// <summary>
    /// Configures the context to use Model Extras for tracking and migrating PostgreSQL triggers and functions.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseModelExtras<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)optionsBuilder).UseModelExtras();
        return optionsBuilder;
    }
}
