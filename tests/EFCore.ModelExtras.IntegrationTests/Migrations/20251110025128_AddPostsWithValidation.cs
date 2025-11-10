using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EFCore.ModelExtras.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class AddPostsWithValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                });

            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION validate_post_content()\n  RETURNS trigger\n  LANGUAGE plpgsql\nAS $function$\nBEGIN\n    IF LENGTH(NEW.content) < 10 THEN\n        RAISE EXCEPTION 'Post content must be at least 10 characters';\n    END IF;\n    RETURN NEW;\nEND;\n$function$");

            migrationBuilder.Sql("CREATE OR REPLACE TRIGGER tu_post_validate_content\n    BEFORE INSERT OR UPDATE OF content\n    ON posts\n    FOR EACH ROW\n    EXECUTE FUNCTION validate_post_content()\n;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER tu_post_validate_content ON posts;");

            migrationBuilder.Sql("DROP FUNCTION validate_post_content;");

            migrationBuilder.DropTable(
                name: "posts");
        }
    }
}
