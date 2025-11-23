using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using EFCore.ModelExtras.Core.ModelDiffer;
using EFCore.ModelExtras.FunctionsAndTriggers.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.FunctionsAndTriggers.Migrations;

internal sealed class FunctionModelDiffer : AbstractSqlOperationModelDiffer<IModel, FunctionDeclaration>
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

        // If target is null (e.g., reverting to migration "0"), generate DROP operations for all source functions
        if (targetModel is null && sourceModel is not null)
        {
            return GetDeclarations(sourceModel)
                .Select(func => DeleteSqlOperation(sourceModel, func));
        }

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
            .Where(a => a.Name.StartsWith(ModelExtrasAnnotations.Key.DeclareFunction))
            .Select(a => {
                if (a is { Value: string functionAnnotationValue }) {
                    return JsonSerializer.Deserialize<FunctionDeclaration>(functionAnnotationValue)
                        ?? throw new InvalidOperationException("Function annotation was not convertible to a FunctionDeclaration.");
                }

                throw new InvalidOperationException("Function annotation was not convertible to a FunctionDeclaration.");
            })
        ?? Enumerable.Empty<FunctionDeclaration>();
}
