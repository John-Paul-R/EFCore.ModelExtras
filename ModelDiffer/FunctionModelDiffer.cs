using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Newtonsoft.Json;
using Jp.Entities.Models.DbContext.Design;
using Jp.Entities.Models.DbContext.Design.Operation;

namespace EFCoreUtility.ModelDiffer;

public sealed class FunctionModelDiffer : AbstractSqlOperationModelDiffer<IModel, FunctionDeclaration>
{
    // We're going to assume that users are providing CREATE OR REPLACE sql...
    // because otherwise this gets hairy (e.g. consider the case where a
    // function is depended on by triggers, then gets deleted. We'd need to
    // somehow traverse all dependent triggers.)
    protected override bool ExplicitDeleteOnDiff => false;

    public override IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target)
    {
        var sourceModel = source?.Model;
        var targetModel = target?.Model;

        return targetModel is null
            ? Array.Empty<MigrationOperation>()
            : BuildMigrationOperationsForModelModify(sourceModel, targetModel);
    }

    protected override SqlOperation DeleteSqlOperation(IModel oldModel, FunctionDeclaration functionDeclaration)
    {
        return new DropFunctionOperation
        {
            Sql = $@"DROP FUNCTION {functionDeclaration.Name};",
            SuppressTransaction = false,
            IsDestructiveChange = true,
        };
    }

    protected override SqlOperation CreateSqlOperation(IModel newModel, FunctionDeclaration functionDeclaration)
    {
        return new AddFunctionOperation
        {
            Sql = functionDeclaration.Source,
            SuppressTransaction = false,
            IsDestructiveChange = true,
        };
    }

    protected override IEnumerable<FunctionDeclaration> GetDeclarations(IModel? entityType)
        => entityType?.GetAnnotations()
            .Where(a => a.Name.StartsWith(JpEfAnnotation.Key.DeclareFunction))
            .Select(a => {
                if (a is { Value: string functionAnnotationValue }) {
                    return JsonConvert.DeserializeObject<FunctionDeclaration>(functionAnnotationValue)
                        ?? throw new InvalidOperationException("Function annotation was not convertible to a FunctionDeclaration.");
                }

                throw new InvalidOperationException("Function annotation was not convertible to a FunctionDeclaration.");
            })
        ?? Enumerable.Empty<FunctionDeclaration>();
}
