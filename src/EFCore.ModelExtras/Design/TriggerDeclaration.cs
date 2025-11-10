using System.Collections.Generic;
using System.Linq;

namespace EFCore.ModelExtras;

/// <summary>
/// Represents a PostgreSQL trigger declaration to be tracked and migrated by EF Core.
/// </summary>
/// <param name="Name">The name of the trigger.</param>
/// <param name="TriggerTiming">When the trigger fires (BEFORE, AFTER, or INSTEAD OF).</param>
/// <param name="TriggerEvent">The events that fire the trigger.</param>
/// <param name="Source">The trigger SQL source, starting after the trigger name in a CREATE TRIGGER statement.</param>
/// <param name="IsConstraintTrigger">Whether this is a constraint trigger.</param>
public record TriggerDeclaration(
        string Name,
        PgTriggerTiming TriggerTiming,
        PgTriggerEventClause[] TriggerEvent,
        string Source,
        bool IsConstraintTrigger = false)
    : SqlObjectDeclaration(Name)
{
    public override string UniqueKey { get; } = Name;
}

public enum PgTriggerTiming
{
    Before,
    After,
    InsteadOf,
}

public enum PgTriggerEvent
{
    Insert,
    Update,
    Delete,
}

public enum PgTriggerExecuteFor
{
    Statement,
    EachRow,
}

public record PgTriggerEventClause(PgTriggerEvent Event, string[]? ColumnsForUpdate)
{
    public static PgTriggerEventClause Insert() => new(PgTriggerEvent.Insert, null);
    public static PgTriggerEventClause Update(params string[] columns) => new(PgTriggerEvent.Update, columns);
    public static PgTriggerEventClause Delete() => new(PgTriggerEvent.Delete, null);

    private PgTriggerEventClause? _next;

    public PgTriggerEventClause Or(PgTriggerEventClause next)
    {
        Enumerate().Last()._next = next;
        return this;
    }

    internal IEnumerable<PgTriggerEventClause> Enumerate()
    {
        PgTriggerEventClause? cur = this;
        while (cur is not null) {
            yield return cur;
            cur = cur._next;
        }
    }

    public string SingleToSql() => Event switch
    {
        PgTriggerEvent.Insert => "INSERT",
        PgTriggerEvent.Update when ColumnsForUpdate is null or {Length: 0} => "UPDATE",
        PgTriggerEvent.Update => $"UPDATE OF {string.Join(' ', ColumnsForUpdate)}",
        PgTriggerEvent.Delete => "DELETE",
    };
}
