using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
using Jp.Entities.Models.DbContext.Design.Operation;

namespace Jp.Entities.Models.DbContext.Design;

public class JpMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public JpMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
#pragma warning disable EF1001 // internal ef core api usage
        INpgsqlSingletonOptions npgsqlSingletonOptions)
#pragma warning restore EF1001
    : base(dependencies, npgsqlSingletonOptions)
    { }

    protected override void Generate(
        MigrationOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        if (operation is AddDeferrableForeignKeyOperation addForeignKeyOperation) {
            GenerateForeignKey(addForeignKeyOperation, model, builder);
        } else {
            base.Generate(operation, model, builder);
        }
    }

    private void GenerateForeignKey(
        AddDeferrableForeignKeyOperation addForeignKeyOperation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        var deferMode = addForeignKeyOperation.DeferMode;
        if (deferMode.IsDeferrable()) {
            GenerateDeferrableForeignKey(addForeignKeyOperation,
                model,
                builder,
                initiallyDeferred: deferMode == JpEfAnnotation.DeferMode.DeferrableInitiallyDeferred);
        } else {
            base.Generate(addForeignKeyOperation, model, builder);
        }
    }

    private void GenerateDeferrableForeignKey(
        AddForeignKeyOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true,
        bool initiallyDeferred = true)
    {
        builder
            .Append("ALTER TABLE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema))
            .Append(" ADD ");

        ForeignKeyConstraint(operation, model, builder);

        builder
            .AppendLine()
            .Append(" DEFERRABLE ")
            .Append(initiallyDeferred ? "INITIALLY DEFERRED" : "INITIALLY IMMEDIATE");

        if (terminate) {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            EndStatement(builder);
        }
    }
}
