using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.ModelExtras.Core;

/// <summary>
/// Extension methods for configuring ModelExtras in a DbContext.
/// </summary>
public static class ModelExtrasServiceExtensions
{
    /// <summary>
    /// Configures the DbContext to use ModelExtras extensibility, allowing plugins to extend
    /// EF Core's migration capabilities.
    /// </summary>
    /// <param name="optionsBuilder">The options builder for the context.</param>
    /// <param name="configure">Action to configure plugins and differs.</param>
    /// <returns>The options builder for method chaining.</returns>
    public static DbContextOptionsBuilder UseModelExtras(
        this DbContextOptionsBuilder optionsBuilder,
        Action<ModelExtrasOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new ModelExtrasOptions();
        configure(options);

        // Replace EF Core's IMigrationsModelDiffer with our composite differ
        optionsBuilder.ReplaceService<IMigrationsModelDiffer, CompositeModelDiffer>();

        // Store options in the service collection for the CompositeModelDiffer to access
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
            .AddOrUpdateExtension(new ModelExtrasOptionsExtension(options));

        return optionsBuilder;
    }
}

/// <summary>
/// Extension to store ModelExtras configuration in DbContextOptions.
/// </summary>
internal class ModelExtrasOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public ModelExtrasOptionsExtension(ModelExtrasOptions options)
    {
        Options = options;
    }

    public ModelExtrasOptions Options { get; }

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Register a service that the CompositeModelDiffer can use to access registered differs
        services.AddSingleton<IModelExtrasRegistrationService>(
            new ModelExtrasRegistrationService(Options));
    }

    public void Validate(IDbContextOptions options)
    {
        // No validation needed
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        private new ModelExtrasOptionsExtension Extension => (ModelExtrasOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using ModelExtras";

        public override int GetServiceProviderHashCode() => Extension.Options.GetHashCode();

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["ModelExtras:PluginCount"] = Extension.Options.Registrations.Count.ToString();
        }
    }
}

/// <summary>
/// Service interface to provide differ registrations to the CompositeModelDiffer.
/// </summary>
public interface IModelExtrasRegistrationService
{
    ModelExtrasOptions Options { get; }
}

/// <summary>
/// Implementation of the registration service.
/// </summary>
public class ModelExtrasRegistrationService : IModelExtrasRegistrationService
{
    public ModelExtrasRegistrationService(ModelExtrasOptions options)
    {
        Options = options;
    }

    public ModelExtrasOptions Options { get; }
}
