using EFCore.ModelExtras.Core;
using EFCore.ModelExtras.FunctionsAndTriggers.Migrations;

namespace EFCore.ModelExtras.FunctionsAndTriggers;

/// <summary>
/// Plugin that adds PostgreSQL function and trigger migration support to EF Core.
/// </summary>
public class FunctionsAndTriggersPlugin : IModelExtrasPlugin
{
    public void RegisterDiffers(IModelExtrasPluginBuilder builder)
    {
        // Functions are foundational - lower priority means they drop last and create first
        builder.RegisterDiffer(new FunctionModelDiffer(), priority: 5);

        // Triggers depend on functions - higher priority means they drop first and create last
        builder.RegisterDiffer(new TriggerModelDiffer(), priority: 10);
    }
}
