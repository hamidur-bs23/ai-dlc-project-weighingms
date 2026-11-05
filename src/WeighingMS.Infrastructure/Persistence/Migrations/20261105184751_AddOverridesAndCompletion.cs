using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighingMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOverridesAndCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "weighing_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completed_by",
                table: "weighing_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "overridden_at",
                table: "weighing_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "override_grant_id",
                table: "weighing_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "override_reason",
                table: "weighing_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "override_supervisor_badge_id",
                table: "weighing_records",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "override_supervisor_name",
                table: "weighing_records",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "surplus_percent",
                table: "weighing_records",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "surplus_unit",
                table: "weighing_records",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "surplus_value",
                table: "weighing_records",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_weighing_sessions_status_completed_at",
                table: "weighing_sessions",
                columns: new[] { "status", "completed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_weighing_records_override_grant_id",
                table: "weighing_records",
                column: "override_grant_id",
                unique: true,
                filter: "override_grant_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_weighing_sessions_status_completed_at",
                table: "weighing_sessions");

            migrationBuilder.DropIndex(
                name: "ix_weighing_records_override_grant_id",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "weighing_sessions");

            migrationBuilder.DropColumn(
                name: "completed_by",
                table: "weighing_sessions");

            migrationBuilder.DropColumn(
                name: "overridden_at",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "override_grant_id",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "override_reason",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "override_supervisor_badge_id",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "override_supervisor_name",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "surplus_percent",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "surplus_unit",
                table: "weighing_records");

            migrationBuilder.DropColumn(
                name: "surplus_value",
                table: "weighing_records");
        }
    }
}
