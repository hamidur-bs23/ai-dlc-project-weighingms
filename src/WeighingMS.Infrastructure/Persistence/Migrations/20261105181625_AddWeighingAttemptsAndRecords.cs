using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighingMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeighingAttemptsAndRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "attempt_started_at",
                table: "weighing_session_components",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attempt_started_by",
                table: "weighing_session_components",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "container",
                table: "weighing_session_components",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lot",
                table: "weighing_session_components",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tare_scale_id",
                table: "weighing_session_components",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tare_unit",
                table: "weighing_session_components",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tare_value",
                table: "weighing_session_components",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "tared_at",
                table: "weighing_session_components",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "weighing_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    chemical_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    chemical_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    @class = table.Column<string>(name: "class", type: "character varying(16)", maxLength: 16, nullable: false),
                    tolerance_source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scale_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scale_class = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    lot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    container = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    operator_badge_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    component_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    required_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    required_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    tare_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tare_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    tolerance_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tolerance_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weighing_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_weighing_records_weighing_session_components_component_id",
                        column: x => x.component_id,
                        principalTable: "weighing_session_components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_weighing_records_weighing_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "weighing_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weighing_records_batch_id",
                table: "weighing_records",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_weighing_records_component_id",
                table: "weighing_records",
                column: "component_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weighing_records_session_id_sequence",
                table: "weighing_records",
                columns: new[] { "session_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weighing_records");

            migrationBuilder.DropColumn(
                name: "attempt_started_at",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "attempt_started_by",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "container",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "lot",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "tare_scale_id",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "tare_unit",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "tare_value",
                table: "weighing_session_components");

            migrationBuilder.DropColumn(
                name: "tared_at",
                table: "weighing_session_components");
        }
    }
}
