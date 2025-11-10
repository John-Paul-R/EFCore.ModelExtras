using EFCoreUtility.ModelDiffer;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Jp.Core.Extensions;
using Jp.Entities.Models.DbContext.Design;
using Jp.Entities.Models.DbContext.Design.Operation;

namespace EFCoreUtility;

#pragma warning disable EF1001
public class JpMigrationsModelDiffer : MigrationsModelDiffer
{
    private readonly IRelationalModelDiffer _triggerModelDiffer;
    private readonly IRelationalModelDiffer _functionModelDiffer;
    private readonly IRelationalModelDiffer _pgCronModelDiffer;

    public JpMigrationsModelDiffer(
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
        _pgCronModelDiffer = new PgCronModelDiffer();
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
            .PartitionOn(op => op is AddFunctionOperation);

        var (addTriggerOps, dropTriggerOps) = _triggerModelDiffer
            .GetOperations(source, target)
            .PartitionOn(op => op is AddTriggerOperation);

        var (addPgCronJobOps, dropPgCronJobOps) = _pgCronModelDiffer
            .GetOperations(source, target)
            .PartitionOn(op => op is PgCronScheduleOperation);

        return Enumerable.Empty<MigrationOperation>()
            .Concat(dropTriggerOps)
            .Concat(dropFunctionOps)
            .Concat(baseDifferences)
            .Concat(addFunctionOps)
            .Concat(addTriggerOps)
            .Concat(addPgCronJobOps)
            .Concat(dropPgCronJobOps)
            .ToList();
    }

    protected override IEnumerable<MigrationOperation> Add(IForeignKeyConstraint target, DiffContext diffContext)
    {
        return base.Add(target, diffContext).Select(op => {
            op.AddAnnotations(JpCustomMigrationsAnnotationProvider.ForAdd(target));
            return op;
        });
    }

    // Some custom logic for regenerating foreign keys if any of our custom
    // foreign key annotations change. (because these annotation changes may
    // represent a change in the generated SQL, since we have made modifications
    // to that logic)
    protected override IEnumerable<MigrationOperation> Diff(
        IForeignKeyConstraint source,
        IForeignKeyConstraint target,
        DiffContext diffContext)
    {
        var targetAnnotations = JpCustomMigrationsAnnotationProvider.ForAdd(target).ToList();

        var baseOperations = base.Diff(source, target, diffContext);
        bool hadBaseOperations = false;
        foreach (var op in baseOperations) {
            hadBaseOperations = true;
            op.AddAnnotations(targetAnnotations);
        }

        if (hadBaseOperations) {
            // At time of writing, this base.Diff overload always returns an Enumerable.Empty<MigrationOperation>().
            // I am currently operating under the assumption if EF ever adds a `Diff` implementation that returns
            // records under some condition, all we have to do in that case is make sure the relevant operations are
            // annotated with our custom annotations. (The alternative is risking generating duplicate operations below)
            // Its likely that if they're generating "Remove" operations, the added annotations will be unnecessary, but
            // it won't break anything if we attach them.
            yield break;
        }

        var sourceAnnotations = JpCustomMigrationsAnnotationProvider.ForAdd(source);
        var sourceSet = sourceAnnotations.Select(a => (a.Name, a.Value)).Order();
        var targetSet = targetAnnotations.Select(a => (a.Name, a.Value)).Order();

        if (sourceSet.SequenceEqual(targetSet)) {
            yield break;
        }

        // Despite the types, these 2 will always return size-1 enumerables
        foreach (var removeOperation in Remove(source, diffContext)) {
            yield return removeOperation;
        }

        foreach (var addOperation in Add(target, diffContext)) {
            yield return addOperation;
        }
    }

    protected override IEnumerable<MigrationOperation> Remove(IForeignKeyConstraint target, DiffContext diffContext)
    {
        Lazy<bool> isDbIgnored = new(() => JpCustomMigrationsAnnotationProvider.ForAdd(target)
            .Any(a => a.Name == JpEfAnnotation.Key.DatabaseGeneratedNever));
        return base.Remove(target, diffContext)
            .Where(_ => !isDbIgnored.Value);
    }
}
