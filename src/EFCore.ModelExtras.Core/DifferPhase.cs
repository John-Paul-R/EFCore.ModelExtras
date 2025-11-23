namespace EFCore.ModelExtras.Core;

/// <summary>
/// Defines the execution phase for differ operations relative to EF Core's built-in operations.
/// Used to control the order of operations in migrations.
/// </summary>
public enum DifferPhase
{
    /// <summary>
    /// Operations that should execute before EF Core's operations (e.g., dropping triggers before dropping tables).
    /// </summary>
    DropPhase,

    /// <summary>
    /// EF Core's built-in migration operations (table creation, column modifications, etc.).
    /// </summary>
    CorePhase,

    /// <summary>
    /// Operations that should execute after EF Core's operations (e.g., creating triggers after tables exist).
    /// </summary>
    CreatePhase
}
