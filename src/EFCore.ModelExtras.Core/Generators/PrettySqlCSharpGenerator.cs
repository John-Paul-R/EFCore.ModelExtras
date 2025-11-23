using EFCore.ModelExtras.Core.Operations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Core.Generators;

/// <summary>
/// C# migration code generator that formats PrettySqlOperation with raw string literals
/// for improved readability in generated migration files.
/// </summary>
public class PrettySqlCSharpGenerator : CSharpMigrationOperationGenerator
{
    public PrettySqlCSharpGenerator(CSharpMigrationOperationGeneratorDependencies dependencies)
        : base(dependencies)
    { }

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
}
