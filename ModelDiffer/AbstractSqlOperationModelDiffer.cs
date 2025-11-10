using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Newtonsoft.Json;
using Jp.Entities.Models.DbContext.Design;

namespace EFCoreUtility.ModelDiffer;

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

    public abstract IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target);

    protected abstract IEnumerable<TDeclaration> GetDeclarations(
        TDeclarer? modelItem);

    protected abstract SqlOperation CreateSqlOperation(
        TDeclarer declaringModelItem,
        TDeclaration triggerDeclaration);

    protected abstract SqlOperation DeleteSqlOperation(
        TDeclarer declaringModelItem,
        TDeclaration triggerDeclaration);

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
        foreach (var commonTriggerName in commonNames)
        {
            var oldValue = oldKeysToDeclarations[commonTriggerName];
            var newValue = newKeysToDeclarations[commonTriggerName];

            if (JsonConvert.SerializeObject(oldValue) != JsonConvert.SerializeObject(newValue)) {
                if (ExplicitDeleteOnDiff) {
                    // commonNames will be an empty array if oldModelItem was null
                    yield return DeleteSqlOperation(oldModelItem!, oldValue);
                }

                yield return CreateSqlOperation(newModelItem, newValue);
            }
        }

        // If declaration was removed, delete it.
        foreach (var oldTriggerName in oldKeysToDeclarations.Keys.Except(commonNames))
        {
            var oldTriggerAnnotation = oldKeysToDeclarations[oldTriggerName];

            // oldKeysToDeclarations will be an empty array if oldModelItem was null
            yield return DeleteSqlOperation(oldModelItem!, oldTriggerAnnotation);
        }

        // If declaration was added, create it.
        foreach (var newTriggerName in newKeysToDeclarations.Keys.Except(commonNames))
        {
            var newTriggerAnnotation = newKeysToDeclarations[newTriggerName];

            yield return CreateSqlOperation(newModelItem, newTriggerAnnotation);
        }
    }

}
