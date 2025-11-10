using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EFCore.ModelExtras.Example.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OldEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NewEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION log_user_email_change()\n  RETURNS trigger\n  LANGUAGE plpgsql\nAS $function$\nBEGIN\n    -- Only log if email actually changed\n    IF (TG_OP = 'UPDATE' AND OLD.email IS DISTINCT FROM NEW.email) THEN\n        INSERT INTO email_audit_logs (user_id, old_email, new_email, changed_at)\n        VALUES (NEW.id, OLD.email, NEW.email, NOW());\n    END IF;\n\n    RETURN NEW;\nEND;\n$function$");

            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION update_timestamp()\n  RETURNS trigger\n  LANGUAGE plpgsql\nAS $function$\nBEGIN\n    NEW.updated_at = NOW();\n    RETURN NEW;\nEND;\n$function$");

            migrationBuilder.Sql("CREATE OR REPLACE TRIGGER tu_user_email_audit\n    AFTER UPDATE OF email\n    ON Users\n    FOR EACH ROW\n    EXECUTE FUNCTION log_user_email_change()\n;");

            migrationBuilder.Sql("CREATE OR REPLACE TRIGGER tu_user_update_timestamp\n    BEFORE UPDATE\n    ON Users\n    FOR EACH ROW\n    EXECUTE FUNCTION update_timestamp()\n;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER tu_user_email_audit ON Users;");

            migrationBuilder.Sql("DROP TRIGGER tu_user_update_timestamp ON Users;");

            migrationBuilder.DropTable(
                name: "EmailAuditLogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
