using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Core.ModelDiffer;

/// <summary>
/// Interface for specialized model differs that can detect changes for specific SQL objects (functions, triggers, etc.).
/// Implementations are registered with the composite differ to extend EF Core's migration generation.
/// </summary>
public interface IRelationalModelDiffer
{
    /// <summary>
    /// Gets the migration operations needed to transform the source model to the target model.
    /// </summary>
    /// <param name="source">The old/source relational model (null for initial migration).</param>
    /// <param name="target">The new/target relational model (null when removing database).</param>
    /// <returns>A sequence of migration operations representing the changes.</returns>
    IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target);
}
