using System;
using System.Collections.Generic;
using System.Linq;
using EFCore.ModelExtras.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using EFCore.ModelExtras;
using EFCore.ModelExtras.Operations;

namespace EFCore.ModelExtras.Migrations;

#pragma warning disable EF1001
internal sealed class ModelExtrasModelDiffer : MigrationsModelDiffer
{
    private readonly IRelationalModelDiffer _triggerModelDiffer;
    private readonly IRelationalModelDiffer _functionModelDiffer;

    public ModelExtrasModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies)
    : base(typeMappingSource,
        migrationsAnnotationProvider,
        rowIdentityMapFactory,
        commandBatchPreparerDependencies)
    {
        _triggerModelDiffer = new TriggerModelDiffer();
        _functionModelDiffer = new FunctionModelDiffer();
    }

    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
    {
        // Order matters here because triggers can depend on functions. (and in
        // the vast majority of cases, the converse won't be true).
        // Because of this, the order must be:
        // - drop triggers
        // - drop functions
        // - normal migration ops
        // - add functions
        // - add triggers

        var baseDifferences = base.GetDifferences(source, target);

        var (addFunctionOps, dropFunctionOps) = _functionModelDiffer
            .GetOperations(source, target)
            .SplitOn(op => op is AddFunctionOperation);

        var (addTriggerOps, dropTriggerOps) = _triggerModelDiffer
            .GetOperations(source, target)
            .SplitOn(op => op is AddTriggerOperation);

        return Enumerable.Empty<MigrationOperation>()
            .Concat(dropTriggerOps)
            .Concat(dropFunctionOps)
            .Concat(baseDifferences)
            .Concat(addFunctionOps)
            .Concat(addTriggerOps)
            .ToList();
    }
}
