using System;
using System.Text.Json.Serialization;

namespace EFCore.ModelExtras.Core;

/// <summary>
/// Base class for SQL object declarations (functions, triggers, views, etc.) that can be tracked in EF Core models.
/// </summary>
public abstract record SqlObjectDeclaration
{
    /// <summary>
    /// Creates a new SQL object declaration.
    /// </summary>
    /// <param name="Name">The name of the SQL object.</param>
    /// <exception cref="ArgumentException">Thrown when Name is null, empty, or whitespace.</exception>
    protected SqlObjectDeclaration(string Name)
    {
        if (string.IsNullOrWhiteSpace(Name)) {
            throw new ArgumentException("Cannot be null, empty, or whitespace", nameof(Name));
        }

        this.Name = Name;
    }

    /// <summary>
    /// A unique key used to identify this declaration for comparison during migrations.
    /// </summary>
    [JsonIgnore]
    public abstract string UniqueKey { get; }

    /// <summary>
    /// The name of the SQL object.
    /// </summary>
    public string Name { get; init; }
}
