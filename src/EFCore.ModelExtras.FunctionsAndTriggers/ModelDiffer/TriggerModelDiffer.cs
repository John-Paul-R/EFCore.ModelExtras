using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using EFCore.ModelExtras.Core.ModelDiffer;
using EFCore.ModelExtras.FunctionsAndTriggers.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.FunctionsAndTriggers.Migrations;

internal sealed class TriggerModelDiffer : AbstractTableSqlOperationModelDiffer<TriggerDeclaration>
{
    protected override SqlOperation DeleteSqlOperation(IEntityType entityType, TriggerDeclaration triggerDeclaration)
    {
        return new DropTriggerOperation
        {
            Sql = $@"DROP TRIGGER {triggerDeclaration.Name} ON {entityType.GetTableName()};",
            SuppressTransaction = false,
            IsDestructiveChange = true,
        };
    }

    protected override SqlOperation CreateSqlOperation(IEntityType createdEntityType, TriggerDeclaration triggerDeclaration)
    {
        var constraintText = triggerDeclaration.IsConstraintTrigger ? "CONSTRAINT " : string.Empty;
        var timingText = triggerDeclaration.TriggerTiming.ToString().ToUpperInvariant();
        var eventText = string.Join(" OR ", triggerDeclaration.TriggerEvent.Select(e => e.SingleToSql()));
        return new AddTriggerOperation
        {
            Sql = $"""
                CREATE OR REPLACE {constraintText}TRIGGER {triggerDeclaration.Name}
                    {timingText} {eventText}
                    ON {createdEntityType.GetTableName()}
                    {triggerDeclaration.Source.Replace("\n", "\n    ")/*Increase indent by 1 level for subsequent lines*/}
                ;
                """,
            SuppressTransaction = false,
            IsDestructiveChange = true,
        };
    }

    protected override IEnumerable<TriggerDeclaration> GetDeclarations(IEntityType? entityType)
        => entityType?.GetAnnotations()
            .Where(a => a.Name.StartsWith(ModelExtrasAnnotations.Key.HasTrigger))
            .Select(a => {
                if (a is { Value: string triggerAnnotationValue }) {
                    return JsonSerializer.Deserialize<TriggerDeclaration>(triggerAnnotationValue)
                        ?? throw new InvalidOperationException("Trigger annotation was not convertible to a TriggerDeclaration.");
                }

                throw new InvalidOperationException("Trigger annotation was not convertible to a TriggerDeclaration.");
            })
        ?? Enumerable.Empty<TriggerDeclaration>();
}
