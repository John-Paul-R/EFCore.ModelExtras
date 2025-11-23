using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Core.ModelDiffer;

/// <summary>
/// Base class for model differs that detect changes to SQL objects scoped to entity types/tables (e.g., triggers).
/// Specialization of AbstractSqlOperationModelDiffer for table-level SQL objects.
/// </summary>
/// <typeparam name="TDeclaration">The type of declaration being tracked (must derive from SqlObjectDeclaration).</typeparam>
public abstract class AbstractTableSqlOperationModelDiffer<TDeclaration>
: AbstractSqlOperationModelDiffer<IEntityType, TDeclaration>
where TDeclaration : SqlObjectDeclaration
{
    /// <summary>
    /// Extracts all declarations from the given entity type.
    /// </summary>
    protected abstract override IEnumerable<TDeclaration> GetDeclarations(
        IEntityType? modelItem);

    /// <summary>
    /// Creates a migration operation to add/create a SQL object on a table.
    /// </summary>
    protected abstract override SqlOperation CreateSqlOperation(
        IEntityType declaringModelItem,
        TDeclaration declaration);

    /// <summary>
    /// Creates a migration operation to drop/delete a SQL object from a table.
    /// </summary>
    protected abstract override SqlOperation DeleteSqlOperation(
        IEntityType declaringModelItem,
        TDeclaration declaration);

    /// <summary>
    /// Gets the migration operations needed to transform the source model to the target model.
    /// Handles entity type additions, deletions, and modifications.
    /// </summary>
    public override IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target)
    {
        var sourceModel = source?.Model;
        var targetModel = target?.Model;

        var oldEntityTypeNames = sourceModel?.GetEntityTypes().Select(e => e.Name).ToHashSet() ?? new HashSet<string>();
        var newEntityTypeNames = targetModel?.GetEntityTypes().Select(e => e.Name).ToHashSet() ?? new HashSet<string>();

        var commonEntityTypeNames = oldEntityTypeNames
            .Intersect(newEntityTypeNames)
            .ToArray();

        var deleteOperations = sourceModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildOperationsForEntityDelete(sourceModel, oldEntityTypeNames.Except(commonEntityTypeNames));

        var addOperations = targetModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildOperationsForEntityCreate(targetModel, newEntityTypeNames.Except(commonEntityTypeNames));

        var modifyOperations = sourceModel is null || targetModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildOperationsForEntityModify(sourceModel, targetModel, commonEntityTypeNames);

        return Enumerable.Empty<MigrationOperation>()
            .Concat(deleteOperations)
            .Concat(addOperations)
            .Concat(modifyOperations);
    }

    private IEnumerable<MigrationOperation> BuildOperationsForEntityDelete(
        IModel sourceModel,
        IEnumerable<string> deletedEntityTypeNames)
    {
        foreach (var deletedTypeName in deletedEntityTypeNames)
        {
            var deletedEntityType = sourceModel.FindEntityType(deletedTypeName)!;

            foreach (var declaration in GetDeclarations(deletedEntityType)) {
                yield return DeleteSqlOperation(deletedEntityType, declaration);
            }
        }
    }

    private IEnumerable<MigrationOperation> BuildOperationsForEntityCreate(
        IModel targetModel,
        IEnumerable<string> createdEntityTypeNames)
    {
        foreach (var newTypeName in createdEntityTypeNames)
        {
            var createdEntityType = targetModel.FindEntityType(newTypeName)!;

            foreach (var declaration in GetDeclarations(createdEntityType)) {
                yield return CreateSqlOperation(createdEntityType, declaration);
            }
        }
    }

    private IEnumerable<MigrationOperation> BuildOperationsForEntityModify(
        IModel sourceModel,
        IModel targetModel,
        IEnumerable<string> commonEntityTypeNames)
    {
        foreach (var entityTypeName in commonEntityTypeNames) {
            var oldEntityType = sourceModel.FindEntityType(entityTypeName)!;
            var newEntityType = targetModel.FindEntityType(entityTypeName)!;

            foreach (var migrationOperation in BuildMigrationOperationsForModelModify(oldEntityType, newEntityType)) {
                yield return migrationOperation;
            }
        }
    }

}
