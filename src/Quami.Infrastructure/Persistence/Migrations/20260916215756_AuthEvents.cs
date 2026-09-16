using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Quami.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    tenant_short_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    attempted_user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auth_events_attempted_user_name_timestamp",
                table: "auth_events",
                columns: new[] { "attempted_user_name", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_events_tenant_id_timestamp",
                table: "auth_events",
                columns: new[] { "tenant_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_events_timestamp",
                table: "auth_events",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_events");
        }
    }
}
