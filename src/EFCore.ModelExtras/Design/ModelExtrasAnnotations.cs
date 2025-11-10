using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;

namespace EFCore.ModelExtras;

/// <summary>
/// Extension methods for configuring PostgreSQL triggers and functions in Entity Framework Core models.
/// </summary>
public static class ModelExtrasAnnotations
{
    /// <summary>
    /// Annotation keys used by the Model Extras library.
    /// </summary>
    public static class Key
    {
        private const string JpPrefix = "Jp_";
        /// <summary>
        /// Annotation key for trigger declarations.
        /// </summary>
        public const string HasTrigger = $"{JpPrefix}HasTrigger";
        /// <summary>
        /// Annotation key for function declarations.
        /// </summary>
        public const string DeclareFunction = $"{JpPrefix}DeclareFunction";
    }

#region HasTrigger
    /// <summary>
    /// Configures a PostgreSQL trigger for the entity type with custom SQL source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type.</param>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="triggerTiming">When the trigger fires (BEFORE, AFTER, or INSTEAD OF).</param>
    /// <param name="triggerEvents">The events that fire the trigger (INSERT, UPDATE, DELETE).</param>
    /// <param name="source">The trigger SQL source, starting after the trigger name in a CREATE TRIGGER statement.</param>
    /// <param name="isConstraintTrigger">Whether this is a constraint trigger.</param>
    /// <returns>The same builder instance.</returns>
    public static EntityTypeBuilder<T> HasTrigger<T>(
        this EntityTypeBuilder<T> entityTypeBuilder,
        string name,
        PgTriggerTiming triggerTiming,
        PgTriggerEventClause[] triggerEvents,
        string source,
        bool isConstraintTrigger = false)
        where T : class
    {
        var triggerDeclaration = new TriggerDeclaration(
            name,
            triggerTiming,
            triggerEvents,
            source,
            isConstraintTrigger);
        entityTypeBuilder.Metadata.AddAnnotation(
            Key.HasTrigger + '_' + name,
            NormalizeLineEndings(JsonConvert.SerializeObject(triggerDeclaration)));

        return entityTypeBuilder;
    }

    public static EntityTypeBuilder<T> HasTrigger<T>(
        this EntityTypeBuilder<T> entityTypeBuilder,
        string name,
        PgTriggerTiming triggerTiming,
        PgTriggerEventClause triggerEvent,
        string source,
        bool isConstraintTrigger = false)
    where T : class
    => HasTrigger(
        entityTypeBuilder,
        name,
        triggerTiming,
        new [] {triggerEvent},
        source,
        isConstraintTrigger);

    /// <summary>
    /// Configures a PostgreSQL trigger for the entity type that executes a specified function.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type.</param>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="triggerTiming">When the trigger fires (BEFORE, AFTER, or INSTEAD OF).</param>
    /// <param name="triggerEvents">The events that fire the trigger (INSERT, UPDATE, DELETE).</param>
    /// <param name="executeFor">Whether to execute for each statement or each row.</param>
    /// <param name="functionToExecute">The function to execute when the trigger fires.</param>
    /// <param name="functionArgs">Optional arguments to pass to the function.</param>
    /// <param name="when">Optional WHEN condition for the trigger.</param>
    /// <param name="isConstraintTrigger">Whether this is a constraint trigger.</param>
    /// <returns>The same builder instance.</returns>
    public static EntityTypeBuilder<T> HasTrigger<T>(
        this EntityTypeBuilder<T> entityTypeBuilder,
        string name,
        PgTriggerTiming triggerTiming,
        PgTriggerEventClause[] triggerEvents,
        PgTriggerExecuteFor executeFor,
        FunctionDeclaration functionToExecute,
        string functionArgs = "",
        string? when = null,
        bool isConstraintTrigger = false)
    where T : class
    {
        var executeForStr = executeFor switch
        {
            PgTriggerExecuteFor.Statement => "STATEMENT",
            PgTriggerExecuteFor.EachRow   => "ROW",
        };
        if (when is not null && string.IsNullOrWhiteSpace(when)) {
            throw new ArgumentException("empty string or whitespace provided", nameof(when));
        }

        return HasTrigger(
            entityTypeBuilder,
            name,
            triggerTiming,
            triggerEvents,
            source: $"""
                FOR EACH {executeForStr}{FmtWhenExpr(when)}
                EXECUTE FUNCTION {functionToExecute.Name}({functionArgs})
                """,
            isConstraintTrigger
        );

        static string FmtWhenExpr(string? when)
        {
            if (when is null) {
                return "";
            }

            if (when.Contains('\n')) {
                return '\n' + $"""
                    WHEN (
                        {when.Replace("\n", "\n    ") /*Increase indent by 1 level for these lines*/}
                    )
                    """;
            }

            return $"\nWHEN ({when})";
        }
    }

    public static EntityTypeBuilder<T> HasTrigger<T>(
        this EntityTypeBuilder<T> entityTypeBuilder,
        string name,
        PgTriggerTiming triggerTiming,
        PgTriggerEventClause triggerEvent,
        PgTriggerExecuteFor executeFor,
        FunctionDeclaration functionToExecute,
        string functionArgs = "",
        string? when = null,
        bool isConstraintTrigger = false)
    where T : class
    => HasTrigger(
        entityTypeBuilder,
        name,
        triggerTiming,
        triggerEvent.Enumerate().ToArray(),
        executeFor,
        functionToExecute,
        functionArgs,
        when,
        isConstraintTrigger
    );
#endregion HasTrigger

#region DeclareFunction
    /// <summary>
    /// Declares a PostgreSQL function in the model to be created and migrated.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="name">The name of the function.</param>
    /// <param name="source">The SQL source code to create the function.</param>
    /// <returns>The same model builder instance.</returns>
    public static ModelBuilder DeclareFunction(
        this ModelBuilder modelBuilder,
        string name,
        [StringSyntax("sql")] string source)
    => DeclareFunction(modelBuilder, new FunctionDeclaration(name, source));

    private static string NormalizeLineEndings(string str)
    {
        return str.Replace(@"\r\n", @"\n");
    }

    /// <summary>
    /// Declares a PostgreSQL function in the model to be created and migrated.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="functionDeclaration">The function declaration.</param>
    /// <returns>The same model builder instance.</returns>
    public static ModelBuilder DeclareFunction(
        this ModelBuilder modelBuilder,
        FunctionDeclaration functionDeclaration)
    {
        modelBuilder.Model.AddAnnotation(
            $"{Key.DeclareFunction}_{functionDeclaration.Name}{functionDeclaration.OverloadDiscriminator}",
            NormalizeLineEndings(JsonConvert.SerializeObject(functionDeclaration)));
        return modelBuilder;
    }
#endregion DeclareFunction
}

public abstract record SqlObjectDeclaration
{
    protected SqlObjectDeclaration(string Name)
    {
        if (string.IsNullOrWhiteSpace(Name)) {
            throw new ArgumentException("Cannot be null, empty, or whitespace", nameof(Name));
        }

        this.Name = Name;
    }

    [JsonIgnore]
    public abstract string UniqueKey { get; }
    public string Name { get; init; }
}
