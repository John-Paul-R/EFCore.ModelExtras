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

namespace Jp.Entities.Models.DbContext.Design;

public static class JpEfAnnotation
{
    public static class Key
    {
        private const string JpPrefix = "Jp_";
        // public const string DatabaseDeleteBehaviorOverride = $"{JpPrefix}DatabaseDeleteBehaviorOverride";
        // public const string DatabaseGeneratedNever = $"{JpPrefix}DatabaseGeneratedNever";
        public const string HasTrigger = $"{JpPrefix}HasTrigger";
        public const string DeclareFunction = $"{JpPrefix}DeclareFunction";
        // public const string DefinePgCronJob = $"{JpPrefix}DefinePgCronJob";
        // public const string Deferrable = $"{JpPrefix}Deferrable";

        // Reflect out the list of 'public const string's defined above. This
        // should realistically only be invoked by design-time migration-builder
        // code, so 1) performance difference w/ static list is irrelevant 2)
        // this class's members should never actually heap-allocate anything at
        // app runtime
        public static IEnumerable<string> GetAll() => typeof(Key)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!);
    }

// #region DatabaseDeleteBehaviorOverride
//     public enum DatabaseDeleteBehavior
//     {
//         NoAction = ReferentialAction.NoAction,
//         Cascade = ReferentialAction.Cascade,
//         SetNull = ReferentialAction.SetNull,
//     }

//     public static ReferentialAction? GetDatabaseDeleteBehaviorOverride(this IMutableAnnotatable operation)
//     {
//         DatabaseDeleteBehavior? val = (DatabaseDeleteBehavior?)operation[Key.DatabaseDeleteBehaviorOverride];
//         return val is null ? null : (ReferentialAction)val.Value;
//     }

//     public static ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> SetDatabaseDeleteBehaviorOverride<TPrincipalEntity, TDependentEntity>(
//         this ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> referenceCollectionBuilder,
//         DatabaseDeleteBehavior databaseDeleteBehavior)
//     where TPrincipalEntity : class
//     where TDependentEntity : class
//     {
//         referenceCollectionBuilder.Metadata.AddAnnotation(Key.DatabaseDeleteBehaviorOverride, databaseDeleteBehavior);
//         return referenceCollectionBuilder;
//     }

//     public static ReferenceReferenceBuilder<TEntity, TRelatedEntity> SetDatabaseDeleteBehaviorOverride<TEntity, TRelatedEntity>(
//         this ReferenceReferenceBuilder<TEntity, TRelatedEntity> referenceReferenceBuilder,
//         DatabaseDeleteBehavior databaseDeleteBehavior)
//     where TEntity : class
//     where TRelatedEntity : class
//     {
//         referenceReferenceBuilder.Metadata.AddAnnotation(Key.DatabaseDeleteBehaviorOverride, databaseDeleteBehavior);
//         return referenceReferenceBuilder;
//     }
// #endregion DatabaseDeleteBehaviorOverride

// #region DatabaseGeneratedNever
//     // See related logic in JpMigrationCSharpGenerator and JpMigrationsModelDiffer
//     public static string? GetDatabaseGeneratedNeverReason(this IMutableAnnotatable operation)
//     {
//         return (string?)operation[Key.DatabaseGeneratedNever];
//     }

//     public static ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> DatabaseGeneratedNever<TPrincipalEntity, TDependentEntity>(
//         this ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> referenceCollectionBuilder,
//         string reason)
//     where TPrincipalEntity : class
//     where TDependentEntity : class
//     {
//         referenceCollectionBuilder.Metadata.AddAnnotation(Key.DatabaseGeneratedNever, reason);
//         return referenceCollectionBuilder;
//     }

//     public static ReferenceReferenceBuilder<TEntity, TRelatedEntity> DatabaseGeneratedNever<TEntity, TRelatedEntity>(
//         this ReferenceReferenceBuilder<TEntity, TRelatedEntity> referenceReferenceBuilder,
//         string reason)
//     where TEntity : class
//     where TRelatedEntity : class
//     {
//         referenceReferenceBuilder.Metadata.AddAnnotation(Key.DatabaseGeneratedNever, reason);
//         return referenceReferenceBuilder;
//     }
// #endregion DatabaseGeneratedNever

#region HasTrigger
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
    public static ModelBuilder DeclareFunction(
        this ModelBuilder modelBuilder,
        string name,
        [StringSyntax("sql")] string source)
    => DeclareFunction(modelBuilder, new FunctionDeclaration(name, source));

    private static string NormalizeLineEndings(string str)
    {
        return str.Replace(@"\r\n", @"\n");
    }

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

// #region DefinePgCronJob
//     public static ModelBuilder DefinePgCronJob(
//         this ModelBuilder modelBuilder,
//         string name,
//         [StringSyntax("cron")] string cronExpression,
//         [StringSyntax("sql")] string source)
//     => DefinePgCronJob(modelBuilder, new PgCronJobDefinition(name, cronExpression, source));

//     public static ModelBuilder DefinePgCronJob(
//         this ModelBuilder modelBuilder,
//         PgCronJobDefinition jobDefinition)
//     {
//         modelBuilder.Model.AddAnnotation(
//             $"{Key.DefinePgCronJob}_{jobDefinition.Name}",
//             NormalizeLineEndings(JsonConvert.SerializeObject(jobDefinition)));
//         return modelBuilder;
//     }
// #endregion DefinePgCronJob

// #region DEFERRABLE
//     public enum DeferMode
//     {
//         NotDeferrable = default,
//         DeferrableInitiallyDeferred,
//         DeferrableInitiallyImmediate,
//     }

//     public static bool IsDeferrable(this DeferMode deferMode)
//         => deferMode is DeferMode.DeferrableInitiallyDeferred or DeferMode.DeferrableInitiallyImmediate;

//     public static DeferMode GetConstraintDeferMode(this IMutableAnnotatable operation)
//     {
//         var annotatedMode = operation[Key.Deferrable];
//         return annotatedMode is null
//             ? default
//             : (DeferMode)annotatedMode;
//     }

//     public static ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> SetDeferrable<TPrincipalEntity, TDependentEntity>(
//         this ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> referenceCollectionBuilder,
//         DeferMode mode)
//         where TPrincipalEntity : class
//         where TDependentEntity : class
//     {
//         referenceCollectionBuilder.Metadata.AddAnnotation(Key.Deferrable, mode);
//         return referenceCollectionBuilder;
//     }

//     public static ReferenceReferenceBuilder<TEntity, TRelatedEntity> SetDeferrable<TEntity, TRelatedEntity>(
//         this ReferenceReferenceBuilder<TEntity, TRelatedEntity> referenceReferenceBuilder,
//         DeferMode mode)
//         where TEntity : class
//         where TRelatedEntity : class
//     {
//         referenceReferenceBuilder.Metadata.AddAnnotation(Key.Deferrable, mode);
//         return referenceReferenceBuilder;
//     }
// #endregion DEFERRABLE
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
