using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace EFCore.ModelExtras.Core;

/// <summary>
/// A composite model differ that orchestrates multiple plugin differs alongside EF Core's built-in differ.
/// Extends MigrationsModelDiffer to maintain compatibility with EF Core's migration pipeline.
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage - necessary for extensibility
public class CompositeModelDiffer : MigrationsModelDiffer
#pragma warning restore EF1001
{
    private readonly List<DifferRegistration> _registrations = new();

    /// <summary>
    /// Creates a new composite model differ.
    /// </summary>
    public CompositeModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies,
        IModelExtrasRegistrationService? registrationService = null)
        : base(
            typeMappingSource,
            migrationsAnnotationProvider,
            rowIdentityMapFactory,
            commandBatchPreparerDependencies)
    {
        // Register all differs from plugins
        if (registrationService != null)
        {
            foreach (var registration in registrationService.Options.Registrations)
            {
                RegisterDiffer(registration);
            }
        }
    }

    /// <summary>
    /// Registers a differ with the specified phase and priority.
    /// </summary>
    internal void RegisterDiffer(DifferRegistration registration)
    {
        _registrations.Add(registration);
    }

    /// <summary>
    /// Gets differences between source and target models, orchestrating both EF Core's operations
    /// and all registered plugin operations in the correct phase order.
    /// </summary>
    public override IReadOnlyList<MigrationOperation> GetDifferences(
        IRelationalModel? source,
        IRelationalModel? target)
    {
        // Get EF Core's built-in differences
        var coreOperations = base.GetDifferences(source, target);

        // Get operations from all registered differs
        var dropPhaseOps = GetOperationsForPhase(source, target, DifferPhase.DropPhase)
            .OrderByDescending(reg => reg.registration.Priority)
            .SelectMany(reg => reg.operations);

        var createPhaseOps = GetOperationsForPhase(source, target, DifferPhase.CreatePhase)
            .OrderBy(reg => reg.registration.Priority)
            .SelectMany(reg => reg.operations);

        // Combine in correct order: Drop -> Core -> Create
        var allOperations = dropPhaseOps
            .Concat(coreOperations)
            .Concat(createPhaseOps)
            .ToList();

        return allOperations;
    }

    private IEnumerable<(DifferRegistration registration, IEnumerable<MigrationOperation> operations)> GetOperationsForPhase(
        IRelationalModel? source,
        IRelationalModel? target,
        DifferPhase phase)
    {
        return _registrations
            .Where(r => r.Phase == phase)
            .Select(r => (r, r.Differ.GetOperations(source, target)));
    }
}
