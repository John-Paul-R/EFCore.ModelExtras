using System;
using System.Collections.Generic;
using EFCore.ModelExtras.Core.ModelDiffer;

namespace EFCore.ModelExtras.Core;

/// <summary>
/// Configuration options for ModelExtras plugins.
/// </summary>
public class ModelExtrasOptions : IModelExtrasPluginBuilder
{
    private readonly List<DifferRegistration> _registrations = new();

    /// <summary>
    /// Gets all registered differs.
    /// </summary>
    internal IReadOnlyList<DifferRegistration> Registrations => _registrations;

    /// <summary>
    /// Registers a plugin that can contribute differs to the migration pipeline.
    /// </summary>
    /// <param name="plugin">The plugin to register.</param>
    /// <returns>The options for method chaining.</returns>
    public ModelExtrasOptions AddPlugin(IModelExtrasPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        plugin.RegisterDiffers(this);
        return this;
    }

    /// <summary>
    /// Registers a model differ for both drop and create phases with the given priority.
    /// </summary>
    /// <param name="differ">The differ that detects changes for SQL objects.</param>
    /// <param name="priority">Priority level representing dependency depth (higher = more dependent on other objects).</param>
    /// <returns>The builder for method chaining.</returns>
    public IModelExtrasPluginBuilder RegisterDiffer(
        IRelationalModelDiffer differ,
        int priority = 0)
    {
        if (differ == null)
            throw new ArgumentNullException(nameof(differ));

        // Register for both phases - the CompositeModelDiffer will handle ordering
        _registrations.Add(new DifferRegistration(differ, DifferPhase.DropPhase, priority));
        _registrations.Add(new DifferRegistration(differ, DifferPhase.CreatePhase, priority));
        return this;
    }

    /// <summary>
    /// Registers a model differ to run in the specified phase with the given priority.
    /// </summary>
    /// <param name="differ">The differ that detects changes for SQL objects.</param>
    /// <param name="phase">The execution phase relative to EF Core's operations.</param>
    /// <param name="priority">Priority within the phase (higher values execute first within drop phase, lower values execute first within create phase).</param>
    /// <returns>The builder for method chaining.</returns>
    public IModelExtrasPluginBuilder RegisterDiffer(
        IRelationalModelDiffer differ,
        DifferPhase phase,
        int priority = 0)
    {
        if (differ == null)
            throw new ArgumentNullException(nameof(differ));

        _registrations.Add(new DifferRegistration(differ, phase, priority));
        return this;
    }
}
