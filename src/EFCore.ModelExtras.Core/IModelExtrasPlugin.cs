using EFCore.ModelExtras.Core.ModelDiffer;

namespace EFCore.ModelExtras.Core;

/// <summary>
/// Interface for plugins that extend EF Core's migration capabilities.
/// Plugins can register differs that detect changes for custom SQL objects (functions, triggers, views, etc.).
/// </summary>
public interface IModelExtrasPlugin
{
    /// <summary>
    /// Registers the plugin's model differs with the provided configuration builder.
    /// </summary>
    /// <param name="builder">The builder used to register differs and their execution phases.</param>
    void RegisterDiffers(IModelExtrasPluginBuilder builder);
}

/// <summary>
/// Builder interface for registering model differs with specific execution phases and priorities.
/// </summary>
public interface IModelExtrasPluginBuilder
{
    /// <summary>
    /// Registers a model differ for both drop and create phases with the given priority.
    /// The differ will automatically execute in the correct order:
    /// - Drop phase: Higher priority values drop first (dependent objects removed before dependencies)
    /// - Create phase: Higher priority values create last (dependencies created before dependent objects)
    /// </summary>
    /// <param name="differ">The differ that detects changes for SQL objects.</param>
    /// <param name="priority">Priority level representing dependency depth (higher = more dependent on other objects).</param>
    /// <returns>The builder for method chaining.</returns>
    IModelExtrasPluginBuilder RegisterDiffer(
        IRelationalModelDiffer differ,
        int priority = 0);

    /// <summary>
    /// Registers a model differ to run in a specific phase with the given priority.
    /// Use this overload for advanced scenarios where you need phase-specific control.
    /// </summary>
    /// <param name="differ">The differ that detects changes for SQL objects.</param>
    /// <param name="phase">The execution phase relative to EF Core's operations.</param>
    /// <param name="priority">Priority within the phase (higher values execute first within drop phase, lower values execute first within create phase).</param>
    /// <returns>The builder for method chaining.</returns>
    IModelExtrasPluginBuilder RegisterDiffer(
        IRelationalModelDiffer differ,
        DifferPhase phase,
        int priority = 0);
}
