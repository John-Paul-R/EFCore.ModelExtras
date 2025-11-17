using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
using EFCore.ModelExtras.Operations;

namespace EFCore.ModelExtras.Migrations;

public sealed class ModelExtrasSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public ModelExtrasSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        INpgsqlSingletonOptions npgsqlSingletonOptions)
        : base(dependencies, npgsqlSingletonOptions)
    {
    }

    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        if (operation is SqlOperation sqlOperation) {
            builder
                .Append(sqlOperation.Sql)
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                .EndCommand();
            return;
        }

        base.Generate(operation, model, builder);
    }
}
