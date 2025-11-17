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

            migrationBuilder.Sql(/*lang=sql*/"""
                CREATE OR REPLACE FUNCTION log_user_email_change()
                  RETURNS trigger
                  LANGUAGE plpgsql
                AS $function$
                BEGIN
                    -- Only log if email actually changed
                    IF (TG_OP = 'UPDATE' AND OLD.email IS DISTINCT FROM NEW.email) THEN
                        INSERT INTO email_audit_logs (user_id, old_email, new_email, changed_at)
                        VALUES (NEW.id, OLD.email, NEW.email, NOW());
                    END IF;

                    RETURN NEW;
                END;
                $function$
                """);

            migrationBuilder.Sql(/*lang=sql*/"""
                CREATE OR REPLACE FUNCTION update_timestamp()
                  RETURNS trigger
                  LANGUAGE plpgsql
                AS $function$
                BEGIN
                    NEW.updated_at = NOW();
                    RETURN NEW;
                END;
                $function$
                """);

            migrationBuilder.Sql(/*lang=sql*/"""
                CREATE OR REPLACE TRIGGER tu_user_email_audit
                    AFTER UPDATE OF email
                    ON Users
                    FOR EACH ROW
                    EXECUTE FUNCTION log_user_email_change()
                ;
                """);

            migrationBuilder.Sql(/*lang=sql*/"""
                CREATE OR REPLACE TRIGGER tu_user_update_timestamp
                    BEFORE UPDATE
                    ON Users
                    FOR EACH ROW
                    EXECUTE FUNCTION update_timestamp()
                ;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER tu_user_email_audit ON Users;");

            migrationBuilder.Sql("DROP TRIGGER tu_user_update_timestamp ON Users;");

            migrationBuilder.Sql("DROP FUNCTION log_user_email_change;");

            migrationBuilder.Sql("DROP FUNCTION update_timestamp;");

            migrationBuilder.DropTable(
                name: "EmailAuditLogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
