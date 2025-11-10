using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Jp.Entities.Models.DbContext.Design;

namespace EFCoreUtility.ModelDiffer;

public abstract class AbstractTableSqlOperationModelDiffer<TDeclaration>
: AbstractSqlOperationModelDiffer<IEntityType, TDeclaration>
where TDeclaration : SqlObjectDeclaration
{
    protected abstract override IEnumerable<TDeclaration> GetDeclarations(
        IEntityType? modelItem);

    protected abstract override SqlOperation CreateSqlOperation(
        IEntityType declaringModelItem,
        TDeclaration triggerDeclaration);

    protected abstract override SqlOperation DeleteSqlOperation(
        IEntityType declaringModelItem,
        TDeclaration triggerDeclaration);

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

        var deleteTriggerOperations = sourceModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildOperationsForEntityDelete(sourceModel, oldEntityTypeNames.Except(commonEntityTypeNames));

        var addTriggerOperations = targetModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildOperationsForEntityCreate(targetModel, newEntityTypeNames.Except(commonEntityTypeNames));

        var modifyTriggerOperations = sourceModel is null || targetModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildOperationsForEntityModify(sourceModel, targetModel, commonEntityTypeNames);

        return Enumerable.Empty<MigrationOperation>()
            .Concat(deleteTriggerOperations)
            .Concat(addTriggerOperations)
            .Concat(modifyTriggerOperations);
    }

    private IEnumerable<MigrationOperation> BuildOperationsForEntityDelete(
        IModel sourceModel,
        IEnumerable<string> deletedEntityTypeNames)
    {
        foreach (var deletedTypeName in deletedEntityTypeNames)
        {
            var deletedEntityType = sourceModel.FindEntityType(deletedTypeName)!;

            foreach (var triggerDeclaration in GetDeclarations(deletedEntityType)) {
                yield return DeleteSqlOperation(deletedEntityType, triggerDeclaration);
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

            foreach (var triggerDeclaration in GetDeclarations(createdEntityType)) {
                yield return CreateSqlOperation(createdEntityType, triggerDeclaration);
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
