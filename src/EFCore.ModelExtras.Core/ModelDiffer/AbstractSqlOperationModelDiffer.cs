using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Core.ModelDiffer;

/// <summary>
/// Base class for model differs that detect changes to SQL object declarations (functions, triggers, etc.).
/// </summary>
/// <typeparam name="TDeclarer">The type that declares the SQL objects (e.g., IModel, IEntityType).</typeparam>
/// <typeparam name="TDeclaration">The type of declaration being tracked (must derive from SqlObjectDeclaration).</typeparam>
public abstract class AbstractSqlOperationModelDiffer<TDeclarer, TDeclaration>
: IRelationalModelDiffer
where TDeclarer : IReadOnlyAnnotatable, IAnnotatable
where TDeclaration : SqlObjectDeclaration
{
    /// <summary>
    /// Whether to delete + create the relation if a diff is detected. If false,
    /// it's assumed that the overridden SQL-generation function for Creating
    /// the relation will be in some sort of "CREATE OR REPLACE" format.
    /// </summary>
    protected virtual bool ExplicitDeleteOnDiff => true;

    /// <summary>
    /// Gets the migration operations needed to transform the source model to the target model.
    /// </summary>
    public abstract IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target);

    /// <summary>
    /// Extracts all declarations from the given model item.
    /// </summary>
    /// <param name="modelItem">The model item to extract declarations from (null if item doesn't exist).</param>
    /// <returns>A sequence of declarations found in the model item.</returns>
    protected abstract IEnumerable<TDeclaration> GetDeclarations(
        TDeclarer? modelItem);

    /// <summary>
    /// Creates a migration operation to add/create a SQL object.
    /// </summary>
    /// <param name="declaringModelItem">The model item that declares this SQL object.</param>
    /// <param name="declaration">The declaration to create.</param>
    /// <returns>A migration operation that creates the SQL object.</returns>
    protected abstract SqlOperation CreateSqlOperation(
        TDeclarer declaringModelItem,
        TDeclaration declaration);

    /// <summary>
    /// Creates a migration operation to drop/delete a SQL object.
    /// </summary>
    /// <param name="declaringModelItem">The model item that declared this SQL object.</param>
    /// <param name="declaration">The declaration to delete.</param>
    /// <returns>A migration operation that drops the SQL object.</returns>
    protected abstract SqlOperation DeleteSqlOperation(
        TDeclarer declaringModelItem,
        TDeclaration declaration);

    /// <summary>
    /// Builds migration operations when a model item is modified (exists in both old and new models).
    /// Compares declarations by their UniqueKey and generates add/drop operations as needed.
    /// </summary>
    /// <param name="oldModelItem">The old version of the model item (null if it didn't exist before).</param>
    /// <param name="newModelItem">The new version of the model item.</param>
    /// <returns>Migration operations representing the changes.</returns>
    protected IEnumerable<MigrationOperation> BuildMigrationOperationsForModelModify(
        TDeclarer? oldModelItem,
        TDeclarer newModelItem)
    {
        var oldKeysToDeclarations = GetDeclarations(oldModelItem)
            .ToDictionary(td => td.UniqueKey);

        var newKeysToDeclarations = GetDeclarations(newModelItem)
            .ToDictionary(td => td.UniqueKey);

        var commonNames = oldKeysToDeclarations.Keys
            .Intersect(newKeysToDeclarations.Keys)
            .ToArray();

        // If declaration was changed, recreate it.
        foreach (var commonKey in commonNames)
        {
            var oldValue = oldKeysToDeclarations[commonKey];
            var newValue = newKeysToDeclarations[commonKey];

            if (JsonSerializer.Serialize(oldValue) != JsonSerializer.Serialize(newValue)) {
                if (ExplicitDeleteOnDiff) {
                    // commonNames will be an empty array if oldModelItem was null
                    yield return DeleteSqlOperation(oldModelItem!, oldValue);
                }

                yield return CreateSqlOperation(newModelItem, newValue);
            }
        }

        // If declaration was removed, delete it.
        foreach (var oldKey in oldKeysToDeclarations.Keys.Except(commonNames))
        {
            var oldDeclaration = oldKeysToDeclarations[oldKey];

            // oldKeysToDeclarations will be an empty dictionary if oldModelItem was null
            yield return DeleteSqlOperation(oldModelItem!, oldDeclaration);
        }

        // If declaration was added, create it.
        foreach (var newKey in newKeysToDeclarations.Keys.Except(commonNames))
        {
            var newDeclaration = newKeysToDeclarations[newKey];

            yield return CreateSqlOperation(newModelItem, newDeclaration);
        }
    }

}
