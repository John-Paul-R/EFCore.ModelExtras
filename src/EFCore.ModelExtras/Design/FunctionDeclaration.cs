using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace EFCore.ModelExtras;

/// <summary>
/// Represents a PostgreSQL function declaration to be tracked and migrated by EF Core.
/// </summary>
/// <param name="Name">The name of the function.</param>
/// <param name="Source">The SQL source code to create the function.</param>
/// <param name="OverloadDiscriminator">Optional discriminator for function overloads.</param>
public record FunctionDeclaration(
        string Name,
        [StringSyntax("sql")] string Source,
        string OverloadDiscriminator = "")
    : SqlObjectDeclaration(Name)
{
    public override string UniqueKey => $"{Name}{OverloadDiscriminator}";
}

public record CallableFunctionDeclaration(
    string Name,
    [property: JsonIgnore] Func<DatabaseFacade, Task> Invoke,
    [StringSyntax("sql")] string Source,
    string OverloadDiscriminator = "")
    : FunctionDeclaration(Name, Source, OverloadDiscriminator)
{
    public override string UniqueKey => $"{Name}{OverloadDiscriminator}";
}

public record CallableFunctionDeclaration<TParam>(
    string Name,
    [property: JsonIgnore] Func<DatabaseFacade, TParam, Task> Invoke,
    [StringSyntax("sql")] string Source,
    string OverloadDiscriminator = "")
    : FunctionDeclaration(Name, Source, OverloadDiscriminator)
{
    public override string UniqueKey => $"{Name}{OverloadDiscriminator}";
}
