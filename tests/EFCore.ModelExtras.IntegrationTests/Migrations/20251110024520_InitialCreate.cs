using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EFCore.ModelExtras.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    old_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    new_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION log_user_email_change()\n  RETURNS trigger\n  LANGUAGE plpgsql\nAS $function$\nBEGIN\n    IF (TG_OP = 'UPDATE' AND OLD.email IS DISTINCT FROM NEW.email) THEN\n        INSERT INTO email_audit_logs (user_id, old_email, new_email, changed_at)\n        VALUES (NEW.id, OLD.email, NEW.email, NOW());\n    END IF;\n    RETURN NEW;\nEND;\n$function$");

            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION update_timestamp()\n  RETURNS trigger\n  LANGUAGE plpgsql\nAS $function$\nBEGIN\n    NEW.updated_at = NOW();\n    RETURN NEW;\nEND;\n$function$");

            migrationBuilder.Sql("CREATE OR REPLACE TRIGGER tu_user_email_audit\n    AFTER UPDATE OF email\n    ON users\n    FOR EACH ROW\n    EXECUTE FUNCTION log_user_email_change()\n;");

            migrationBuilder.Sql("CREATE OR REPLACE TRIGGER tu_user_update_timestamp\n    BEFORE UPDATE\n    ON users\n    FOR EACH ROW\n    EXECUTE FUNCTION update_timestamp()\n;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER tu_user_email_audit ON users;");

            migrationBuilder.Sql("DROP TRIGGER tu_user_update_timestamp ON users;");

            migrationBuilder.Sql("DROP FUNCTION log_user_email_change;");

            migrationBuilder.Sql("DROP FUNCTION update_timestamp;");

            migrationBuilder.DropTable(
                name: "email_audit_logs");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
