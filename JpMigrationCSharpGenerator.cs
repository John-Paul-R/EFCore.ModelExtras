using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Jp.Entities.Models.DbContext.Design;
using Jp.Entities.Models.DbContext.Design.Operation;

namespace EFCoreUtility;

public sealed class JpMigrationCSharpGenerator : CSharpMigrationOperationGenerator
{
    public JpMigrationCSharpGenerator(CSharpMigrationOperationGeneratorDependencies dependencies)
    : base(dependencies)
    { }

    private ICSharpHelper Code => Dependencies.CSharpHelper;

    protected override void Generate(AddForeignKeyOperation operation, IndentedStringBuilder builder)
    {
        { // DatabaseGeneratedNever
            string? notPersistedToDbReason = operation.GetDatabaseGeneratedNeverReason();
            if (notPersistedToDbReason is not null) {
                builder.Append($".Sql(\"--Skipping CREATE ForeignKey '{operation.Name}', reason: '{notPersistedToDbReason}'\")");
                return;
            }
        }

        { // DatabaseDeleteBehaviorOverride
            var dbDeleteActionOverride = operation.GetDatabaseDeleteBehaviorOverride();
            operation.RemoveAnnotation(JpEfAnnotation.Key.DatabaseDeleteBehaviorOverride);
            if (dbDeleteActionOverride is not null) {
                operation.OnDelete = dbDeleteActionOverride.Value;
            }
        }

        var deferMode = operation.GetConstraintDeferMode();
        // We will process this annotation and translate it into migration code,
        // so it doesn't need to be included in the migration verbatim.
        operation.RemoveAnnotation(JpEfAnnotation.Key.Deferrable);

        if (!deferMode.IsDeferrable()) {
            base.Generate(operation, builder);
            return;
        }

        GenerateDeferrableForeignKey(operation, builder, deferMode);
    }

    protected override void Generate(DropForeignKeyOperation operation, IndentedStringBuilder builder)
    {
        { // DatabaseGeneratedNever
            string? notPersistedToDbReason = operation.GetDatabaseGeneratedNeverReason();
            if (notPersistedToDbReason is not null) {
                builder.Append($".Sql(\"--Skipping DROP ForeignKey '{operation.Name}', reason: '{notPersistedToDbReason}'\")");
                return;
            }
        }

        base.Generate(operation, builder);
    }

    protected override void Generate(SqlOperation operation, IndentedStringBuilder builder)
    {
        if (operation is not PrettySqlOperation) {
            base.Generate(operation, builder);
            return;
        }

        const string TripleQuote = "\"\"\"";
        builder
            .Append(".Sql(")
            .Append("/*lang=sql*/")
            .AppendLine(TripleQuote);

        using (builder.Indent()) {
            foreach (var sqlLine in operation.Sql.Split('\n')) {
                builder.AppendLine(sqlLine);
            }
            builder
                .Append(TripleQuote)
                .Append(")");

            Annotations(operation.GetAnnotations(), builder);
        }
    }

    // taken from the CSharpMigrationOperationGenerator.Generate method matching
    // the 1st 2 params
    private void GenerateDeferrableForeignKey(
        AddForeignKeyOperation operation,
        IndentedStringBuilder builder,
        JpEfAnnotation.DeferMode deferMode)
    {
#region JpCode
        builder.AppendLine(".AddDeferrableForeignKey(");
#endregion JpCode

        using (builder.Indent())
        {
            builder
                .Append("name: ")
                .Append(Code.Literal(operation.Name));

            if (operation.Schema != null)
            {
                builder
                    .AppendLine(",")
                    .Append("schema: ")
                    .Append(Code.Literal(operation.Schema));
            }

            builder
                .AppendLine(",")
                .Append("table: ")
                .Append(Code.Literal(operation.Table))
                .AppendLine(",");

            if (operation.Columns.Length == 1)
            {
                builder
                    .Append("column: ")
                    .Append(Code.Literal(operation.Columns[0]));
            }
            else
            {
                builder
                    .Append("columns: ")
                    .Append(Code.Literal(operation.Columns));
            }

            if (operation.PrincipalSchema != null)
            {
                builder
                    .AppendLine(",")
                    .Append("principalSchema: ")
                    .Append(Code.Literal(operation.PrincipalSchema));
            }

            builder
                .AppendLine(",")
                .Append("principalTable: ")
                .Append(Code.Literal(operation.PrincipalTable));

            if (operation.PrincipalColumns != null)
            {
                if (operation.PrincipalColumns.Length == 1)
                {
                    builder
                        .AppendLine(",")
                        .Append("principalColumn: ")
                        .Append(Code.Literal(operation.PrincipalColumns[0]));
                }
                else
                {
                    builder
                        .AppendLine(",")
                        .Append("principalColumns: ")
                        .Append(Code.Literal(operation.PrincipalColumns));
                }
            }

            if (operation.OnUpdate != ReferentialAction.NoAction)
            {
                builder
                    .AppendLine(",")
                    .Append("onUpdate: ")
                    .Append(Code.Literal(operation.OnUpdate));
            }

            if (operation.OnDelete != ReferentialAction.NoAction)
            {
                builder
                    .AppendLine(",")
                    .Append("onDelete: ")
                    .Append(Code.Literal(operation.OnDelete));
            }
#region JpCode
            builder
                .AppendLine(",")
                .Append("deferMode: ")
                .Append(Code.Literal(deferMode));
#endregion JpCode

            builder.Append(")");

            Annotations(operation.GetAnnotations(), builder);
        }
    }
}
