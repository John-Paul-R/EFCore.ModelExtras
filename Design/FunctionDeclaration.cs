using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace Jp.Entities.Models.DbContext.Design;

/// <param name="Name">The name of the function</param>
/// <param name="Source">The function source</param>
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
