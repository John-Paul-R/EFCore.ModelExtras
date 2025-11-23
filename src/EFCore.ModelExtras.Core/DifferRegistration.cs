using EFCore.ModelExtras.Core.ModelDiffer;

namespace EFCore.ModelExtras.Core;

/// <summary>
/// Stores metadata about a registered differ including its execution phase and priority.
/// </summary>
internal class DifferRegistration
{
    /// <summary>
    /// Creates a new differ registration.
    /// </summary>
    /// <param name="differ">The differ instance.</param>
    /// <param name="phase">The execution phase.</param>
    /// <param name="priority">The priority within the phase.</param>
    public DifferRegistration(IRelationalModelDiffer differ, DifferPhase phase, int priority)
    {
        Differ = differ;
        Phase = phase;
        Priority = priority;
    }

    /// <summary>
    /// The differ that detects changes for SQL objects.
    /// </summary>
    public IRelationalModelDiffer Differ { get; }

    /// <summary>
    /// The execution phase relative to EF Core's operations.
    /// </summary>
    public DifferPhase Phase { get; }

    /// <summary>
    /// Priority within the phase. For DropPhase, higher values execute first.
    /// For CreatePhase, lower values execute first.
    /// </summary>
    public int Priority { get; }
}
