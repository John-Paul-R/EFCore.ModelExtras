using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Newtonsoft.Json;
using Jp.Entities.Models.DbContext.Design;
using Jp.Entities.Models.DbContext.Design.Operation;

namespace EFCoreUtility.ModelDiffer;

public sealed class PgCronModelDiffer : AbstractSqlOperationModelDiffer<IModel, PgCronJobDefinition>
{
    // pg_cron allows named `cron.schedule(name, cron_expr, sql_src)`
    // invocations to replace one another
    protected override bool ExplicitDeleteOnDiff => false;

    public override IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target)
    {
        var sourceModel = source?.Model;
        var targetModel = target?.Model;

        return targetModel is null
            ? []
            : BuildMigrationOperationsForModelModify(sourceModel, targetModel);
    }

    protected override SqlOperation DeleteSqlOperation(IModel oldModel, PgCronJobDefinition jobDefinition)
    {
        if (jobDefinition.JobName.Contains('\'')) {
            throw new InvalidOperationException("job definition name contained a single quote (') -- mistake?");
        }
        return new PgCronUnscheduleOperation
        {
            Sql = $@"SELECT cron.unschedule('{jobDefinition.JobName}');",
            SuppressTransaction = false,
            IsDestructiveChange = true,
        };
    }

    private static string Indent(string str, int level = 1)
    {
        StringBuilder sb = new();
        var indentStr = new string(' ', 4 * level);

        var remainingSpan = str.AsSpan();
        int newlineIdx;

        while ((newlineIdx = remainingSpan.IndexOf('\n')) != -1) {
            sb.Append(indentStr);
            sb.Append(remainingSpan[..newlineIdx]);
            sb.Append('\n');
            remainingSpan = remainingSpan[(newlineIdx + 1)..];
        }

        sb.Append(indentStr);
        sb.Append(remainingSpan);

        return sb.ToString();
    }

    protected override SqlOperation CreateSqlOperation(IModel newModel, PgCronJobDefinition jobDefinition)
    {
        if (jobDefinition.JobName.Contains('\'')) {
            throw new InvalidOperationException("job definition name contained a single quote (') -- mistake?");
        }
        if (jobDefinition.CronExpression.Contains('\'')) {
            throw new InvalidOperationException("job definition cron expression contained a single quote (') -- mistake?");
        }
        return new PgCronScheduleOperation
        {
            Sql = $"""
                SELECT cron.schedule(
                    '{jobDefinition.JobName}',
                    '{jobDefinition.CronExpression}',
                    $__pg_cron_job_body__$
                {Indent(jobDefinition.Source)}
                    $__pg_cron_job_body__$
                );
                """,
            SuppressTransaction = false,
            IsDestructiveChange = true,
        };
    }

    protected override IEnumerable<PgCronJobDefinition> GetDeclarations(IModel? entityType)
        => entityType?.GetAnnotations()
            .Where(a => a.Name.StartsWith(JpEfAnnotation.Key.DefinePgCronJob))
            .Select(a => {
                if (a is { Value: string jobAnnotationValue }) {
                    return JsonConvert.DeserializeObject<PgCronJobDefinition>(jobAnnotationValue)
                        ?? throw new InvalidOperationException("Function annotation was not convertible to a PgCronJobDefinition.");
                }

                throw new InvalidOperationException("Function annotation was not convertible to a PgCronJobDefinition.");
            })
        ?? [];
}
